using System.Net;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Ordering.Application.Models;
using Ordering.Infrastructure.Mail;
using Xunit;

namespace Ordering.API.Test;

public class EmailServiceTests
{
  [Fact]
  public async Task SendEmail_ReturnsTrue_WhenSendGridReturnsAccepted()
  {
    var emailSettings = Options.Create(new EmailSettings { ApiKey = "test-key", FromAddress = "from@test.local", FromName = "Tester" });

    var mockClient = new Mock<ISendGridClientWrapper>();
    mockClient.Setup(c => c.SendEmailAsync(It.IsAny<SendGrid.Helpers.Mail.SendGridMessage>(), It.IsAny<CancellationToken>()))
      .ReturnsAsync(HttpStatusCode.Accepted);

    var svc = new EmailService(emailSettings, NullLogger<EmailService>.Instance, mockClient.Object);

    var email = new Email { To = "to@test.local", Subject = "Hello", Body = "Body" };
    var result = await svc.SendEmail(email, CancellationToken.None);

    Assert.True(result);
  }

  [Fact]
  public async Task SendEmail_ReturnsFalse_WhenSendGridReturnsBadRequest()
  {
    var emailSettings = Options.Create(new EmailSettings { ApiKey = "test-key", FromAddress = "from@test.local", FromName = "Tester" });

    var mockClient = new Mock<ISendGridClientWrapper>();
    mockClient.Setup(c => c.SendEmailAsync(It.IsAny<SendGrid.Helpers.Mail.SendGridMessage>(), It.IsAny<CancellationToken>()))
      .ReturnsAsync(HttpStatusCode.BadRequest);

    var svc = new EmailService(emailSettings, NullLogger<EmailService>.Instance, mockClient.Object);

    var email = new Email { To = "to@test.local", Subject = "Hello", Body = "Body" };
    var result = await svc.SendEmail(email, CancellationToken.None);

    Assert.False(result);
  }
}
