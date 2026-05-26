using System.Net;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Ordering.Application.Contracts.Infrastructure;
using Ordering.Application.Models;
using SendGrid;
using SendGrid.Helpers.Mail;

namespace Ordering.Infrastructure.Mail;

public class EmailService(IOptions<EmailSettings> emailSettings, ILogger<EmailService> logger)
  : IEmailService
{
  public EmailSettings EmailSettings { get; } = emailSettings.Value;
  public ILogger<EmailService> Logger { get; } = logger ?? throw new ArgumentNullException(nameof(logger));

  public async Task<bool> SendEmail(Email email, CancellationToken cancellationToken = default)
  {
    var client = new SendGridClient(EmailSettings.ApiKey);
    var subject = email.Subject;
    var to = new EmailAddress(email.To);
    var emailBody = email.Body;

    var from = new EmailAddress
    {
      Email = EmailSettings.FromAddress,
      Name = EmailSettings.FromName
    };

    var sendGridMessage = MailHelper.CreateSingleEmail(from, to, subject, emailBody, emailBody);
    var response = await client.SendEmailAsync(sendGridMessage, cancellationToken);

    Logger.LogInformation("Email sent.");

    if (response.StatusCode == HttpStatusCode.Accepted || response.StatusCode == HttpStatusCode.OK)
      return true;

    Logger.LogError("Email sending failed.");
    return false;
  }
}