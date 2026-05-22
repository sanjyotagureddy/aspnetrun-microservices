namespace SharedKernel.Middleware;
public sealed class GlobalExceptionHandlerMiddleware(RequestDelegate next, ILogger<GlobalExceptionHandlerMiddleware> logger, string serviceCode)
{
    private readonly RequestDelegate _next = next ?? throw new ArgumentNullException(nameof(next));
    private readonly ILogger<GlobalExceptionHandlerMiddleware> _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    private readonly string _serviceCode = serviceCode ?? string.Empty;

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception ex)
    {
        if (context.Response.HasStarted)
        {
            _logger.LogWarning("The response has already started, the global exception handler will not be executed.");
            throw ex;
        }

        Error errorPayload;
        int statusCode;

        if (ex is SharedKernel.Exceptions.BaseException be)
        {
            statusCode = be.HttpStatus;
            errorPayload = be.Error;
            _logger.LogWarning(ex, "Handled BaseException: {ErrorCode}", be.ErrorCode);
        }
        else
        {
            statusCode = 500;
            var infos = new List<Info>();
            Exception cur = ex;
            while (cur != null)
            {
                infos.Add(new Info(SharedKernel.Constants.CommonErrorCodes.Unknown, cur.Message ?? string.Empty));
                cur = cur.InnerException;
            }

            var composed = $"{statusCode}-{SharedKernel.Constants.CommonErrorCodes.Unknown}-{_serviceCode}";
            errorPayload = new Error(composed, ex.Message ?? "An unexpected error occurred.", infos.ToArray());
            _logger.LogError(ex, "Unhandled exception: {Message}", ex.Message);
        }

        context.Response.Clear();
        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/json";

        var opts = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
        var json = JsonSerializer.Serialize(errorPayload, opts);
        await context.Response.WriteAsync(json);
    }
}
