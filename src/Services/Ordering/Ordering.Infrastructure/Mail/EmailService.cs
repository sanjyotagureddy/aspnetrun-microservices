﻿using System;
using System.Net;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Ordering.Application.Contracts.Infrastructure;
using Ordering.Application.Models;
using SendGrid;
using SendGrid.Helpers.Mail;

namespace Ordering.Infrastructure.Mail;

public class EmailService : IEmailService
{
  public EmailService(IOptions<EmailSettings> emailSettings, ILogger<EmailService> logger)
  {
    EmailSettings = emailSettings.Value ?? throw new ArgumentNullException(nameof(emailSettings));
    Logger = logger ?? throw new ArgumentNullException(nameof(logger));
  }

  public EmailSettings EmailSettings { get; }
  public ILogger<EmailService> Logger { get; }

  public async Task<bool> SendEmail(Email email)
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
    var response = await client.SendEmailAsync(sendGridMessage);

    Logger.LogInformation("Email sent.");

    if (response.StatusCode == HttpStatusCode.Accepted || response.StatusCode == HttpStatusCode.OK)
      return true;

    Logger.LogError("Email sending failed.");
    return false;
  }
}