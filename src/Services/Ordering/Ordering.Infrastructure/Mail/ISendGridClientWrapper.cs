using System.Net;
using System.Threading;
using System.Threading.Tasks;
using SendGrid.Helpers.Mail;

namespace Ordering.Infrastructure.Mail;

public interface ISendGridClientWrapper
{
  Task<HttpStatusCode> SendEmailAsync(SendGridMessage message, CancellationToken cancellationToken = default);
}
