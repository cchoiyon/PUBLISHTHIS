using Project3.Shared.Utilities;
using Microsoft.AspNetCore.Authentication.Cookies;
using Project3.Shared.Models.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.AspNetCore.Builder;
using System;
using Microsoft.Extensions.Configuration;
using Project3.WebApp.Services;
using Project3.WebApp.Repositories;
using System.IO;
using System.Data.SqlClient;
using System.Threading.Tasks;
using System.Data;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.FileProviders;
using Project3.Shared.Services;

// Import namespace automatically included by ASP.NET Core when Swashbuckle.AspNetCore is referenced

var builder = WebApplication.CreateBuilder(args);

// --- Services ---

// sooo we need to get email settings from appsettings.json
builder.Services.Configure<SmtpSettings>(builder.Configuration.GetSection("SmtpSettings"));

// wait this lets us use the actual object instead of IOptions<> thing
// learned this from stackoverflow
builder.Services.AddSingleton(sp => {
    var settings = new SmtpSettings();
    builder.Configuration.GetSection("SmtpSettings").Bind(settings);
    
    // putting password in secrets, never put passwords in code!!
    settings.Password = builder.Configuration["SmtpSettings:Password"];
    
    return settings;
});

// adding my custom services - hope these work right
builder.Services.AddTransient<Project3.Shared.Utilities.Email>();
builder.Services.AddScoped<Project3.Shared.Utilities.Connection>(); // Changed from DBConnect to Connection
builder.Services.AddScoped<ReservationRepository>(); // Add repository
builder.Services.AddScoped<UserRepository>();
builder.Services.AddScoped<AuthService>();

// Add FileStorage Services
// builder.Services.AddScoped<Project3.Shared.Services.FileStorageService>();
builder.Services.AddScoped<Project3.Shared.Services.FileStorageService>(provider => {
    var hostEnvironment = provider.GetRequiredService<IWebHostEnvironment>();
    var configuration = provider.GetRequiredService<IConfiguration>();
    return new Project3.Shared.Services.FileStorageService(hostEnvironment.ContentRootPath, configuration);
});
builder.Services.AddScoped<Project3.Shared.Repositories.PhotoRepository>();

// this is for gmail - took forever to figure out! 
// finally got it working with app password instead of regular password
// Using Temple Email Service instead
builder.Services.AddSingleton<EmailService>(sp => {
    var logger = sp.GetRequiredService<ILogger<EmailService>>();
    var emailService = sp.GetRequiredService<Email>();
    return new EmailService(
        logger,
        emailService,
        "tuo53004@temple.edu" // Temple email address
    );
});

// client notification service was here but removed it cuz it wasn't working right

// ok this makes charts - finally working after hours of debugging
builder.Services.AddTransient<ChartService>();

// need this for API calls - learned this in class!
builder.Services.AddHttpClient();
// setting up specific client for my own API
builder.Services.AddHttpClient("Project3Api", client =>
{
    // figure out if we're in development or production
    var apiBaseUrl = builder.Environment.IsDevelopment() 
        ? builder.Configuration["ApplicationUrls:ApiBaseUrl"] 
        : builder.Configuration["Production:ApiBaseUrl"];
    
    // make sure this matches where API is running locally or nothing works!
    client.BaseAddress = new Uri(apiBaseUrl);
    client.DefaultRequestHeaders.Add("Accept", "application/json");
    // adding timeout cuz API was hanging sometimes - so annoying
    client.Timeout = TimeSpan.FromSeconds(10);
    
    Console.WriteLine($"API URL configured as: {apiBaseUrl}");
});

// i think this is for dependency injection? not 100% sure but it works
var apiBaseUrl = builder.Environment.IsDevelopment() 
    ? builder.Configuration["ApplicationUrls:ApiBaseUrl"] 
    : builder.Configuration["Production:ApiBaseUrl"];
builder.Services.AddSingleton(new { ApiBaseUrl = apiBaseUrl });

// Adds services for MVC Controllers and Views
builder.Services.AddControllersWithViews();

// Swagger stuff for API testing page
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// this stuff is for caching and session - super important for login!
builder.Services.AddMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// Authentication - using Cookies
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";       // Redirect here if not logged in
        options.LogoutPath = "/Account/Logout";
        options.AccessDeniedPath = "/Account/AccessDenied"; // Redirect here if logged in but not allowed
        options.ExpireTimeSpan = TimeSpan.FromMinutes(60);
        options.SlidingExpiration = true;
        
        // this was super tricky - had a bug where unauthenticated users got sent to AccessDenied 
        // instead of Login, took FOREVER to fix
        options.Events = new CookieAuthenticationEvents
        {
            OnRedirectToAccessDenied = context =>
            {
                // Check if the user is authenticated
                if (!context.HttpContext.User.Identity.IsAuthenticated)
                {
                    // If not authenticated, redirect to login instead of AccessDenied
                    context.Response.Redirect("/Account/Login");
                    return Task.CompletedTask;
                }
                
                // Otherwise proceed with normal AccessDenied handling
                context.Response.Redirect(context.RedirectUri);
                return Task.CompletedTask;
            }
        };
    });

// Authorization services (needed if using [Authorize])
builder.Services.AddAuthorization();

// CORS is confusing but i think this is right??
// needed this to make API requests work from different origins
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowSchoolServer", policy =>
    {
        // Temple University server domains
        policy.WithOrigins(
            "http://cis-mssql1.temple.edu",    // Temple's SQL server
            "https://cis-mssql1.temple.edu",   // Temple's SQL server (HTTPS)
            "http://localhost:5000",           // Local development
            "http://localhost:5001",           // Local development with HTTPS
            "http://127.0.0.1:5000",           // Local development (alternative)
            "http://127.0.0.1:5001"            // Local development with HTTPS (alternative)
        )
        .AllowAnyMethod()
        .AllowAnyHeader()
        .AllowCredentials();
    });
});

builder.Services.AddLogging();

// ==================================================
var app = builder.Build();
// ==================================================

// --- Middleware Pipeline (Order Matters!) ---

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}
else
{
    // Dev mode: show detailed errors and the Swagger UI
    app.UseDeveloperExceptionPage();
    app.UseSwagger();
    app.UseSwaggerUI(); // Makes the /swagger page work
}

// making sure SQL directory exists - gotta have this!
try 
{
    var sqlDirectory = Path.Combine(app.Environment.ContentRootPath, "SQL");
    if (!Directory.Exists(sqlDirectory))
    {
        try 
        {
            Directory.CreateDirectory(sqlDirectory);
        }
        catch (UnauthorizedAccessException ex)
        {
            // Log the error but continue - don't crash the application
            app.Logger.LogWarning($"Could not create SQL directory: {ex.Message}. App will continue without this directory.");
        }
    }
    
    // Ensure FileStorage directory exists
    try 
    {
        // Get the configured path
        var fileStoragePath = builder.Configuration["FileStorage:Path"];
        
        // Remove the tilde if it exists
        if (fileStoragePath.StartsWith("~"))
        {
            fileStoragePath = fileStoragePath.Replace("~", app.Environment.ContentRootPath);
        }
        else
        {
            // If no tilde, make it absolute anyway to avoid confusion
            fileStoragePath = Path.Combine(app.Environment.ContentRootPath, "FileStorage");
        }
        
        // Log the path being used
        app.Logger.LogInformation($"FileStorage path: {fileStoragePath}");
        
        // Try to create the directory if it doesn't exist
        if (!Directory.Exists(fileStoragePath))
        {
            try
            {
                Directory.CreateDirectory(fileStoragePath);
                app.Logger.LogInformation($"Created FileStorage directory at {fileStoragePath}");
            }
            catch (Exception dirEx)
            {
                app.Logger.LogWarning($"Could not create FileStorage directory: {dirEx.Message}. Attempting alternative location.");
                
                // Try an alternative location - wwwroot/FileStorage
                fileStoragePath = Path.Combine(app.Environment.WebRootPath, "FileStorage");
                if (!Directory.Exists(fileStoragePath))
                {
                    Directory.CreateDirectory(fileStoragePath);
                    app.Logger.LogInformation($"Created FileStorage directory in wwwroot at {fileStoragePath}");
                }
            }
        }
        
        // Update the configuration to use the correct path
        builder.Configuration["FileStorage:Path"] = fileStoragePath;
    }
    catch (Exception ex)
    {
        app.Logger.LogWarning($"Error working with FileStorage directory: {ex.Message}. Some features may not work correctly.");
    }
}
catch (Exception ex)
{
    app.Logger.LogWarning($"Error working with directory structure: {ex.Message}. App will continue.");
}

// this part sets up the 2FA columns - spent hours on auth stuff
try
{
    using var scope = app.Services.CreateScope();
    var dbConnection = scope.ServiceProvider.GetRequiredService<Connection>();
    
    // no idea if this is the best way but it works!
    var execSpCmd = new SqlCommand("TP_spEnsure2FAColumns");
    execSpCmd.CommandType = CommandType.StoredProcedure;
    dbConnection.DoUpdateUsingCmdObj(execSpCmd);
    app.Logger.LogInformation("Executed TP_spEnsure2FAColumns to verify 2FA columns");
}
catch (Exception ex)
{
    app.Logger.LogError($"Error setting up 2FA columns: {ex.Message}");
}

// checking for restaurant images table - had to add this after project started
try 
{
    using var scope = app.Services.CreateScope();
    var dbConnection = scope.ServiceProvider.GetRequiredService<Connection>();
    
    // check if the table exists - not sure if this is best way?
    var checkTableCmd = new SqlCommand(@"
        SELECT COUNT(*) 
        FROM INFORMATION_SCHEMA.TABLES 
        WHERE TABLE_SCHEMA = 'dbo' 
        AND TABLE_NAME = 'TP_RestaurantImages'");
    
    var tableResult = dbConnection.ExecuteScalarUsingCmdObj(checkTableCmd);
    var tableExists = Convert.ToInt32(tableResult) > 0;
    
    if (!tableExists)
    {
       
        var createTableSQL = @"
        CREATE TABLE [dbo].[TP_RestaurantImages](
            [ImageID] [int] IDENTITY(1,1) NOT NULL PRIMARY KEY,
            [RestaurantID] [int] NOT NULL,
            [ImagePath] [nvarchar](500) NOT NULL,
            [Caption] [nvarchar](200) NULL,
            [UploadDate] [datetime] NOT NULL DEFAULT GETDATE(),
            [DisplayOrder] [int] NOT NULL DEFAULT 0,
            CONSTRAINT [FK_TP_RestaurantImages_TP_Restaurants] FOREIGN KEY([RestaurantID])
            REFERENCES [dbo].[TP_Restaurants] ([RestaurantID])
            ON DELETE CASCADE
        )";
        
        var createTableCmd = new SqlCommand(createTableSQL);
        var createTableResult = dbConnection.DoUpdateUsingCmdObj(createTableCmd);
        
        app.Logger.LogInformation($"Created TP_RestaurantImages table. Result: {createTableResult}");
    }
    else
    {
        app.Logger.LogInformation("TP_RestaurantImages table already exists.");
    }
}
catch (Exception ex)
{
    app.Logger.LogError($"Error setting up restaurant images table: {ex.Message}");
}

app.UseHttpsRedirection();
app.UseStaticFiles(); // For wwwroot files (CSS, JS)

// Modify the static file middleware section to handle potential errors
try
{
    // Configure additional static file middleware for FileStorage directory
    var fileStoragePath = builder.Configuration["FileStorage:Path"];
    if (Directory.Exists(fileStoragePath))
    {
        app.UseStaticFiles(new StaticFileOptions
        {
            FileProvider = new Microsoft.Extensions.FileProviders.PhysicalFileProvider(fileStoragePath),
            RequestPath = builder.Configuration["FileStorage:WebPath"] ?? "/FileStorage"
        });
        app.Logger.LogInformation($"Configured static file middleware for path: {fileStoragePath}");
    }
    else
    {
        app.Logger.LogWarning($"FileStorage directory not found at {fileStoragePath}. File serving from this location will not work.");
    }
}
catch (Exception ex)
{
    app.Logger.LogError($"Error setting up static file middleware: {ex.Message}");
}

app.UseRouting(); // Decides which endpoint to use

// CORS needs to go here - order matters!!
app.UseCors("AllowSchoolServer");

// Session needs to be configured before Auth and endpoint mapping
app.UseSession();

// Authentication & Authorization
app.UseAuthentication();
app.UseAuthorization();

// trying to fix AccessDenied issue again
app.Use(async (context, next) =>
{
    // Special case for AccessDenied - if user is not authenticated, redirect to login
    if (context.Request.Path.StartsWithSegments("/Account/AccessDenied") && 
        !context.User.Identity.IsAuthenticated)
    {
        app.Logger.LogInformation("Unauthenticated user tried to access AccessDenied, redirecting to login");
        context.Response.Redirect("/Account/Login");
        return;
    }
    
    // adding these specific checks for the pages that need auth
    // not sure if there's a better way but this works
    if ((context.Request.Path.StartsWithSegments("/ReviewerHome") || 
         context.Request.Path.Value == "/") && 
        !context.User.Identity.IsAuthenticated)
    {
        app.Logger.LogInformation("Unauthenticated user tried to access {Path}, redirecting to login", context.Request.Path);
        context.Response.Redirect("/Account/Login");
        return;
    }
    
    await next();
});

// --- Map Endpoints ---

// Maps API controllers (using routes defined in the controller files)
app.MapControllers();

// Maps the default route for MVC pages (controller/action/optional-id)
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// global exception handler - helps catch anything that slips through
// better than getting the yellow screen of death lol
app.Use(async (context, next) =>
{
    try
    {
        await next.Invoke();
    }
    catch (Exception ex)
    {
        app.Logger.LogError(ex, "Unhandled exception in middleware pipeline");
        
        // Only redirect to error page for non-API requests that accept HTML
        if (!context.Request.Path.StartsWithSegments("/api") && 
            context.Request.Headers["Accept"].ToString().Contains("text/html"))
        {
            context.Response.Redirect("/Home/Error");
        }
        else
        {
            context.Response.StatusCode = 500;
        }
    }
});

// ==================================================
app.Run(); // Start the app!
// ==================================================
