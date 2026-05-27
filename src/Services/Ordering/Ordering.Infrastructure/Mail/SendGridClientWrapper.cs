using System.Net;
using SendGrid;
using SendGrid.Helpers.Mail;

namespace Ordering.Infrastructure.Mail;

public class SendGridClientWrapper(string apiKey) : ISendGridClientWrapper
{
    public async Task<HttpStatusCode> SendEmailAsync(SendGridMessage message, CancellationToken cancellationToken = default)
  {
    var client = new SendGridClient(apiKey);
    var response = await client.SendEmailAsync(message, cancellationToken);
    return response.StatusCode;
  }
}
