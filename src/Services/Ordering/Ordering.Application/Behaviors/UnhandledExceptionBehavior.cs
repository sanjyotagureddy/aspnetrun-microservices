using MediatR;
using Microsoft.Extensions.Logging;
using Ordering.Application.Exceptions;

namespace Ordering.Application.Behaviors;

public class UnhandledExceptionBehavior<TRequest, TResponse>(ILogger<TRequest> logger)
  : IPipelineBehavior<TRequest, TResponse>
  where TRequest : IRequest<TResponse>
{
  private readonly ILogger<TRequest> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

  public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next,
    CancellationToken cancellationToken)
  {
    try
    {
      return await next();
    }
    catch (Exception ex)
    {
      var requestName = typeof(TRequest).Name;
      _logger.LogError(ex, $"Application Request: Unhandled Exception for Request {requestName} {request}");
      throw new UnhandledException(requestName, request);
    }
  }
}