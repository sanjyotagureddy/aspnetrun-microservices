using System.Net;
using System.Threading;
using System.Threading.Tasks;
using SendGrid;
using SendGrid.Helpers.Mail;

namespace Ordering.Infrastructure.Mail;

public class SendGridClientWrapper : ISendGridClientWrapper
{
  private readonly string _apiKey;

  public SendGridClientWrapper(string apiKey)
  {
    _apiKey = apiKey;
  }

  public async Task<HttpStatusCode> SendEmailAsync(SendGridMessage message, CancellationToken cancellationToken = default)
  {
    var client = new SendGridClient(_apiKey);
    var response = await client.SendEmailAsync(message, cancellationToken);
    return response.StatusCode;
  }
}
