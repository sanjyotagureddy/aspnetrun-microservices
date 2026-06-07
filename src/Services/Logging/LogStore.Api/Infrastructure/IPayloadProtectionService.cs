namespace LogStore.Api.Infrastructure;

public interface ILogStorageService
{
    Task<CreateLogResponse> CreateAsync(CreateLogRequest request, CancellationToken cancellationToken);

    Task<GetLogResponse> GetAsync(string id, CancellationToken cancellationToken);
}
