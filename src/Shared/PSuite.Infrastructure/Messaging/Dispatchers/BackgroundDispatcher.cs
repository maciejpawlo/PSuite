using System.Collections.Concurrent;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using PSuite.Shared.Abstractions.Messaging;
using PSuite.Shared.Abstractions.Modules;

namespace PSuite.Shared.Infrastructure.Messaging.Dispatchers;

internal sealed class BackgroundDispatcher(
    IMessageChannel messageChannel,
    IModuleClient moduleClient,
    ILogger<BackgroundDispatcher> logger) : BackgroundService
{
    private readonly ConcurrentQueue<IMessage> _followUps = new();

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        DispatchContext.FollowUps = _followUps;

        // Not bound to stoppingToken: on shutdown the writer is completed and the queue drains to the end.
        await foreach (var message in messageChannel.Reader.ReadAllAsync(CancellationToken.None))
        {
            await DispatchAsync(message);

            while (_followUps.TryDequeue(out var followUp))
            {
                await DispatchAsync(followUp);
            }
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        messageChannel.Writer.TryComplete();
        await base.StopAsync(cancellationToken);

        var undelivered = messageChannel.Reader.Count + _followUps.Count;
        if (cancellationToken.IsCancellationRequested && undelivered > 0)
        {
            logger.LogWarning("Shutdown timed out with {UndeliveredCount} queued messages not delivered.", undelivered);
        }
    }

    private async Task DispatchAsync(IMessage message)
    {
        try
        {
            await moduleClient.PublishAsync(message, CancellationToken.None);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Failed to dispatch message {MessageType}.", message.GetType().Name);
        }
    }
}
