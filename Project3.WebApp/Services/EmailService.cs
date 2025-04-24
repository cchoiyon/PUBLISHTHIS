using Project3.Shared.Utilities;

namespace Project3.WebApp.Services
{
    /// <summary>
    /// Service for sending emails using Temple University email server
    /// </summary>
    public class EmailService
    {
        private readonly ILogger<EmailService> _logger;
        private readonly Email _emailService;
        private readonly string _fromAddress;

        /// <summary>
        /// Initializes a new instance of the EmailService class
        /// </summary>
        /// <param name="logger">Logger for recording email operations</param>
        /// <param name="emailService">The underlying email service implementation</param>
        /// <param name="fromAddress">The email address used as the sender</param>
        public EmailService(
            ILogger<EmailService> logger,
            Email emailService, 
            string fromAddress = "tuo53004@temple.edu")
        {
            _logger = logger;
            _emailService = emailService;
            _fromAddress = fromAddress;
        }

        /// <summary>
        /// Sends an email asynchronously
        /// </summary>
        /// <param name="recipient">Email address of the recipient</param>
        /// <param name="subject">Subject line of the email</param>
        /// <param name="body">HTML content of the email</param>
        /// <param name="isHtml">Specifies whether the body contains HTML (default: true)</param>
        /// <returns>A task representing the asynchronous operation</returns>
        public async Task SendEmailAsync(string recipient, string subject, string body, bool isHtml = true)
        {
            try
            {
                await Task.Run(() => _emailService.SendMail(recipient, _fromAddress, subject, body));
                _logger.LogInformation($"Email sent successfully to {recipient}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to send email to {recipient}");
                throw;
            }
        }
    }
} 