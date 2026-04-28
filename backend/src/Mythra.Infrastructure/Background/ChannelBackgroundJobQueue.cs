using System.Threading.Channels;
using Mythra.Application.Abstractions.Background;

namespace Mythra.Infrastructure.Background;

public sealed class ChannelBackgroundJobQueue : IBackgroundJobQueue
{
    private readonly Channel<BackgroundJob> _channel = Channel.CreateUnbounded<BackgroundJob>(new UnboundedChannelOptions
    {
        SingleReader = false,
        SingleWriter = false,
    });

    public async ValueTask EnqueueAsync(BackgroundJob job, CancellationToken ct = default) =>
        await _channel.Writer.WriteAsync(job, ct);

    public IAsyncEnumerable<BackgroundJob> DequeueAllAsync(CancellationToken ct) =>
        _channel.Reader.ReadAllAsync(ct);
}
