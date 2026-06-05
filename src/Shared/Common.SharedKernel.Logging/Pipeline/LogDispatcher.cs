namespace Common.SharedKernel.Logging;

internal sealed class LogDispatcher
{
    private readonly IReadOnlyList<ILogSink> _sinks;
    private readonly LoggingOptions _options;
    private readonly Channel<LogEntry> _channel;

    public LogDispatcher(IReadOnlyList<ILogSink> sinks, IOptions<LoggingOptions> options)
    {
        this._sinks = sinks;
        this._options = options.Value;
        _channel = Channel.CreateBounded<LogEntry>(new BoundedChannelOptions(this._options.QueueCapacity)
        {
            FullMode = this._options.QueueFullMode,
            SingleReader = true,
            SingleWriter = false
        });
    }

    public ValueTask EnqueueAsync(LogEntry entry, CancellationToken cancellationToken = default)
        => _channel.Writer.WriteAsync(entry, cancellationToken);

    public async Task RunAsync(CancellationToken cancellationToken)
    {
        List<LogEntry> batch = new(Math.Max(1, _options.BatchSize));

        try
        {
            while (await _channel.Reader.WaitToReadAsync(cancellationToken).ConfigureAwait(false))
            {
                while (_channel.Reader.TryRead(out LogEntry? entry))
                {
                    batch.Add(entry);

                    if (batch.Count >= _options.BatchSize)
                    {
                        await FlushBatchAsync(batch, cancellationToken).ConfigureAwait(false);
                        batch.Clear();
                    }
                }

                if (batch.Count > 0)
                {
                    await FlushBatchAsync(batch, cancellationToken).ConfigureAwait(false);
                    batch.Clear();
                }
            }
        }
        catch (OperationCanceledException)
        {
        }
        finally
        {
            if (batch.Count > 0)
            {
                await FlushBatchAsync(batch, CancellationToken.None).ConfigureAwait(false);
            }
        }
    }

    private async Task FlushBatchAsync(IReadOnlyList<LogEntry> batch, CancellationToken cancellationToken)
    {
        if (batch.Count is 0 || _sinks.Count is 0)
        {
            return;
        }

        Task[] writeTasks = _sinks.Select(sink => WriteSinkAsync(sink, batch, cancellationToken).AsTask()).ToArray();
        await Task.WhenAll(writeTasks).ConfigureAwait(false);
    }

    private static async ValueTask WriteSinkAsync(ILogSink sink, IReadOnlyList<LogEntry> batch, CancellationToken cancellationToken)
    {
        try
        {
            if (sink is IBulkLogSink bulkSink)
            {
                await bulkSink.WriteBatchAsync(batch, cancellationToken).ConfigureAwait(false);
                return;
            }

            foreach (LogEntry entry in batch)
            {
                await sink.WriteAsync(entry, cancellationToken).ConfigureAwait(false);
            }
        }
        catch
        {
        }
    }
}

internal interface IBulkLogSink
{
    ValueTask WriteBatchAsync(IReadOnlyList<LogEntry> entries, CancellationToken cancellationToken = default);
}
