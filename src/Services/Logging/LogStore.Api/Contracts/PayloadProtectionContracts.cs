using System.Text.Json;

namespace LogStore.Api.Contracts;

public sealed record CreateLogRequest(
    JsonElement Document,
    string? Id = null,
    string? IndexPrefix = null);

public sealed record CreateLogResponse(
    string Id,
    string Index,
    DateTimeOffset StoredAtUtc);

public sealed record GetLogResponse(
    string Id,
    string Index,
    JsonElement Document);
