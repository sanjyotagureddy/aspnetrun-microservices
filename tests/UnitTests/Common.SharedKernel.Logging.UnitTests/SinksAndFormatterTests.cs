using System.Globalization;

namespace Common.SharedKernel.Logging.UnitTests;

public sealed class SinksAndFormatterTests
{
    [Fact]
    public async Task FileLogSink_ShouldWriteAndRollDailyFile()
    {
        string root = Path.Combine(Path.GetTempPath(), "logging-tests", Guid.NewGuid().ToString("N"));
        string basePath = Path.Combine(root, "app.log");

        FileLogSink sink = new(new FileSinkOptions
        {
            FilePath = basePath,
            FormatterKind = LogFormatterKind.Json,
            RollDaily = true
        });

        DateTimeOffset stamp = DateTimeOffset.Parse("2026-06-07T00:00:00Z");
        LogEntry entry = LogEntry.Create(
            LogLevel.Information,
            "Catalog",
            "Catalog.Api",
            "request",
            "created",
            stamp,
            properties: new Dictionary<string, object?> { ["sku"] = "ABC-1" });

        await sink.WriteAsync(entry, CancellationToken.None);

        string expectedFile = Path.Combine(root, "app-20260607.log");
        File.Exists(expectedFile).Should().BeTrue();
        string content = await File.ReadAllTextAsync(expectedFile, CancellationToken.None);
        content.Should().Contain("\"message\":\"created\"");
    }

    [Fact]
    public async Task ConsoleLogSink_ShouldWriteLine()
    {
        ConsoleLogSink sink = new(new ConsoleSinkOptions
        {
            FormatterKind = LogFormatterKind.Text,
            WriteErrorsToStandardError = false
        });

        LogEntry entry = LogEntry.Create(
            LogLevel.Information,
            "Catalog",
            "Catalog.Api",
            "request",
            "console-line",
            DateTimeOffset.UtcNow);

        StringWriter writer = new();
        TextWriter originalOut = Console.Out;
        Console.SetOut(writer);
        try
        {
            await sink.WriteAsync(entry, CancellationToken.None);
        }
        finally
        {
            Console.SetOut(originalOut);
        }

        writer.ToString().Should().Contain("console-line");
    }

    [Fact]
    public async Task ElasticsearchLogSink_WriteBatchAsync_WithEmptyEntries_ShouldReturn()
    {
        ElasticsearchLogSink sink = new(new ElasticsearchSinkOptions
        {
            Endpoint = new Uri("http://localhost:9200"),
            IndexName = "logs"
        });

        await sink.WriteBatchAsync(Array.Empty<LogEntry>(), CancellationToken.None);
    }

    [Fact]
    public void ElasticsearchLogSink_ShouldRouteApiLog_ToApiLogsDailyIndex()
    {
        ElasticsearchLogSink sink = new(new ElasticsearchSinkOptions
        {
            Endpoint = new Uri("http://localhost:9200"),
            ApiIndexPrefix = "api-logs",
            InfraIndexPrefix = "infra-logs",
            UseDailyIndexes = true,
            RouteInfrastructureLogs = true
        });

        LogEntry entry = LogEntry.Create(
            LogLevel.Information,
            "Products.Api",
            "Products.Api.Features.Products.GetProduct",
            "products.get",
            "fetched product",
            DateTimeOffset.Parse("2026-06-07T10:00:00Z", CultureInfo.InvariantCulture));

        string index = sink.ResolveIndexName(entry);

        index.Should().Be("api-logs-2026.06.07");
    }

    [Fact]
    public void ElasticsearchLogSink_ShouldRouteMicrosoftLog_ToInfraLogsDailyIndex()
    {
        ElasticsearchLogSink sink = new(new ElasticsearchSinkOptions
        {
            Endpoint = new Uri("http://localhost:9200"),
            ApiIndexPrefix = "api-logs",
            InfraIndexPrefix = "infra-logs",
            UseDailyIndexes = true,
            RouteInfrastructureLogs = true
        });

        LogEntry entry = LogEntry.Create(
            LogLevel.Warning,
            "Products.Api",
            "Microsoft.AspNetCore.Hosting.Diagnostics",
            "host.lifecycle",
            "application started",
            DateTimeOffset.Parse("2026-06-07T10:00:00Z", CultureInfo.InvariantCulture));

        string index = sink.ResolveIndexName(entry);

        index.Should().Be("infra-logs-2026.06.07");
    }

    [Fact]
    public void TextLogFormatter_ShouldIncludeCorrelationAndException()
    {
        TextLogFormatter formatter = new();
        InvalidOperationException exception = new("boom");
        LogEntry entry = LogEntry.Create(
            LogLevel.Error,
            "Catalog",
            "Catalog.Api",
            "request",
            "failed",
            DateTimeOffset.Parse("2026-06-07T00:00:00Z"),
            "corr-1",
            exception);

        string line = formatter.Format(entry);

        line.Should().Contain("corr-1");
        line.Should().Contain("failed");
        line.Should().Contain("InvalidOperationException");
    }
}