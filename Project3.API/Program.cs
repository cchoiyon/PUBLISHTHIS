using Project3.Shared.Utilities;
using Project3.Shared.Models.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

// Load email settings from appsettings.json if needed
builder.Services.Configure<SmtpSettings>(builder.Configuration.GetSection("SmtpSettings"));

// Register SmtpSettings as a concrete instance (not IOptions)
builder.Services.AddSingleton(sp => {
    var settings = new SmtpSettings();
    builder.Configuration.GetSection("SmtpSettings").Bind(settings);
    
    // Get password from User Secrets or Environment Variables (not in appsettings.json)
    settings.Password = builder.Configuration["SmtpSettings:Password"];
    
    return settings;
});

// Register custom services from Shared project
builder.Services.AddTransient<Project3.Shared.Utilities.Email>();
builder.Services.AddScoped<Project3.Shared.Utilities.Connection>();

// Register notification service - REMOVED

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Add CORS services - Using the configuration that worked before
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowWebApp", policy =>
    {
        policy.WithOrigins(
                "https://localhost:7130", // Local WebApp development with HTTPS
                "http://localhost:5133",    // Local WebApp development
                "https://cis-iis2.temple.edu", // School server
                "https://cis-iis2.temple.edu/Spring2025/CIS3342_tuo53004/termproject", // Your specific app path
                "https://cis-iis2.temple.edu/Spring2025/CIS3342_tuo53004/termproject/" // With trailing slash
            )
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials();
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Use CORS middleware - Updated policy name
app.UseCors("AllowWebApp");

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
