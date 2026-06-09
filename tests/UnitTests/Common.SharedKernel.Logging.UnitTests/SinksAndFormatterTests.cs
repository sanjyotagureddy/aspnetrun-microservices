using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Xunit;

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
            "products-api",
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
            "products-api",
            "Microsoft.AspNetCore.Hosting.Diagnostics",
            "host.lifecycle",
            "application started",
            DateTimeOffset.Parse("2026-06-07T10:00:00Z", CultureInfo.InvariantCulture));

        string index = sink.ResolveIndexName(entry);

        index.Should().Be("infra-logs-2026.06.07");
    }

    [Fact]
    public void ElasticsearchLogSink_ShouldRouteMessagingLog_ToMessagingLogsDailyIndex()
    {
        ElasticsearchLogSink sink = new(new ElasticsearchSinkOptions
        {
            Endpoint = new Uri("http://localhost:9200"),
            ApiIndexPrefix = "api-logs",
            InfraIndexPrefix = "infra-logs",
            MessagingIndexPrefix = "messaging-log",
            UseDailyIndexes = true,
            RouteInfrastructureLogs = true,
            RouteMessagingLogs = true
        });

        LogEntry entry = LogEntry.Create(
            LogLevel.Information,
            "products-api",
            "Common.SharedKernel.Messaging.Kafka.Producers.KafkaMessageProducer",
            "messaging.publish",
            "message published",
            DateTimeOffset.Parse("2026-06-07T10:00:00Z", CultureInfo.InvariantCulture),
            properties: new Dictionary<string, object?>
            {
                ["provider"] = "Kafka"
            });

        string index = sink.ResolveIndexName(entry);

        index.Should().Be("messaging-log-2026.06.07");
    }

    [Fact]
    public void ElasticsearchLogSink_ShouldRouteEventLog_ToEventLogsDailyIndex()
    {
        ElasticsearchLogSink sink = new(new ElasticsearchSinkOptions
        {
            Endpoint = new Uri("http://localhost:9200"),
            MessagingIndexPrefix = "messaging-log",
            UseDailyIndexes = true,
            RouteMessagingLogs = true
        });

        LogEntry entry = LogEntry.Create(
            LogLevel.Information,
            "products-api",
            "Products.Api.Features.Products.PublishEvent",
            "event.product.updated",
            "product updated event",
            DateTimeOffset.Parse("2026-06-07T10:00:00Z", CultureInfo.InvariantCulture),
            properties: new Dictionary<string, object?>
            {
                ["logType"] = "event",
                ["eventName"] = "ProductUpdated",
                ["eventVersion"] = "1"
            });

        string index = sink.ResolveIndexName(entry);

        index.Should().Be("messaging-log-2026.06.07");
    }

    [Fact]
    public void ElasticsearchLogSink_ShouldRoutePluralEventsCategory_ToMessagingLogsDailyIndex()
    {
        ElasticsearchLogSink sink = new(new ElasticsearchSinkOptions
        {
            Endpoint = new Uri("http://localhost:9200"),
            MessagingIndexPrefix = "messaging-log",
            UseDailyIndexes = true,
            RouteMessagingLogs = true
        });

        LogEntry entry = LogEntry.Create(
            LogLevel.Information,
            "products-api",
            "Products.Api.Features.Products.PublishEvent",
            "events.product.updated",
            "product updated event",
            DateTimeOffset.Parse("2026-06-07T10:00:00Z", CultureInfo.InvariantCulture));

        string index = sink.ResolveIndexName(entry);

        index.Should().Be("messaging-log-2026.06.07");
    }

    [Fact]
    public void ElasticsearchLogSink_ShouldRouteEventMetadataOnlyLog_ToMessagingLogsDailyIndex()
    {
        ElasticsearchLogSink sink = new(new ElasticsearchSinkOptions
        {
            Endpoint = new Uri("http://localhost:9200"),
            MessagingIndexPrefix = "messaging-log",
            UseDailyIndexes = true,
            RouteMessagingLogs = true
        });

        LogEntry entry = LogEntry.Create(
            LogLevel.Information,
            "products-api",
            "Products.Api.Features.Products.PublishEvent",
            null,
            "product updated event",
            DateTimeOffset.Parse("2026-06-07T10:00:00Z", CultureInfo.InvariantCulture),
            properties: new Dictionary<string, object?>
            {
                ["eventName"] = "ProductUpdated",
                ["eventVersion"] = "1"
            });

        string index = sink.ResolveIndexName(entry);

        index.Should().Be("messaging-log-2026.06.07");
    }

    [Fact]
    public void ElasticsearchLogSink_ShouldRoutePayloadLog_ToPayloadDailyIndex()
    {
        ElasticsearchLogSink sink = new(new ElasticsearchSinkOptions
        {
            Endpoint = new Uri("http://localhost:9200"),
            ApiIndexPrefix = "api-logs",
            PayloadIndexPrefix = "api-payload",
            UseDailyIndexes = true
        });

        LogEntry entry = LogEntry.Create(
            LogLevel.Information,
            "products-api",
            "Products.Api.Features.Products.GetProduct",
            "request.payload",
            "payload stored",
            DateTimeOffset.Parse("2026-06-07T10:00:00Z", CultureInfo.InvariantCulture),
            properties: new Dictionary<string, object?>
            {
                ["logType"] = "payload"
            });

        string index = sink.ResolveIndexName(entry);

        index.Should().Be("api-payload-2026.06.07");
    }

    [Fact]
    public void ElasticsearchLogSink_ShouldRouteAuditLog_ToAuditLogsDailyIndex()
    {
        ElasticsearchLogSink sink = new(new ElasticsearchSinkOptions
        {
            Endpoint = new Uri("http://localhost:9200"),
            AuditIndexPrefix = "audit-log",
            UseDailyIndexes = true,
            RouteAuditLogs = true
        });

        LogEntry entry = LogEntry.Create(
            LogLevel.Information,
            "products-api",
            "Products.Api.Features.Products.DeleteProduct",
            "audit.product.delete",
            "product deleted",
            DateTimeOffset.Parse("2026-06-07T10:00:00Z", CultureInfo.InvariantCulture),
            properties: new Dictionary<string, object?>
            {
                ["logType"] = "audit",
                ["action"] = "Delete",
                ["resourceType"] = "Product",
                ["resourceId"] = "42",
                ["performedBy"] = "admin"
            });

        string index = sink.ResolveIndexName(entry);

        index.Should().Be("audit-log-2026.06.07");
    }

    [Fact]
    public void ElasticsearchLogSink_ShouldRouteSecurityLog_ToSecurityLogsDailyIndex()
    {
        ElasticsearchLogSink sink = new(new ElasticsearchSinkOptions
        {
            Endpoint = new Uri("http://localhost:9200"),
            SecurityIndexPrefix = "security-log",
            UseDailyIndexes = true,
            RouteSecurityLogs = true
        });

        LogEntry entry = LogEntry.Create(
            LogLevel.Warning,
            "products-api",
            "Products.Api.Security.Authorization",
            "security.authorization.denied",
            "access denied",
            DateTimeOffset.Parse("2026-06-07T10:00:00Z", CultureInfo.InvariantCulture),
            properties: new Dictionary<string, object?>
            {
                ["logType"] = "security",
                ["action"] = "authorize",
                ["result"] = "denied"
            });

        string index = sink.ResolveIndexName(entry);

        index.Should().Be("security-log-2026.06.07");
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