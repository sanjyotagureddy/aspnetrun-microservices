using System.Net;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Ordering.Application.Contracts.Infrastructure;
using Ordering.Application.Models;
using SendGrid.Helpers.Mail;

namespace Ordering.Infrastructure.Mail;

public class EmailService(IOptions<EmailSettings> emailSettings, ILogger<EmailService> logger, ISendGridClientWrapper clientWrapper)
  : IEmailService
{
  public EmailSettings EmailSettings { get; } = emailSettings.Value;
  public ILogger<EmailService> Logger { get; } = logger ?? throw new ArgumentNullException(nameof(logger));
  public ISendGridClientWrapper ClientWrapper { get; } = clientWrapper ?? throw new ArgumentNullException(nameof(clientWrapper));

  public async Task<bool> SendEmail(Email email, CancellationToken cancellationToken = default)
  {
    var subject = email.Subject;
    var to = new EmailAddress(email.To);
    var emailBody = email.Body;

    var from = new EmailAddress
    {
      Email = EmailSettings.FromAddress,
      Name = EmailSettings.FromName
    };

    SendGridMessage sendGridMessage = MailHelper.CreateSingleEmail(from, to, subject, emailBody, emailBody);

    HttpStatusCode status = await ClientWrapper.SendEmailAsync(sendGridMessage, cancellationToken);

    Logger.LogInformation("Email sent.");

    if (status is HttpStatusCode.Accepted or HttpStatusCode.OK)
      return true;

    Logger.LogError("Email sending failed.");
    return false;
  }
}
