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
    private readonly CancellationTokenSource _shutdownTimedOut = new();
    private volatile int _inFlight;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Not bound to stoppingToken: on shutdown the writer is completed and the queue drains to the end.
        await foreach (var message in messageChannel.Reader.ReadAllAsync(CancellationToken.None))
        {
            if (_shutdownTimedOut.IsCancellationRequested)
            {
                break;
            }

            await DispatchAsync(message);
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        messageChannel.Writer.TryComplete();
        await base.StopAsync(cancellationToken);

        if (!cancellationToken.IsCancellationRequested)
        {
            return;
        }

        var undelivered = messageChannel.Reader.Count + _inFlight;
        await _shutdownTimedOut.CancelAsync();

        if (undelivered > 0)
        {
            logger.LogWarning("Shutdown timed out with {UndeliveredCount} messages not delivered.", undelivered);
        }
    }

    private async Task DispatchAsync(IMessage message)
    {
        _inFlight = 1;
        try
        {
            await moduleClient.PublishAsync(message, _shutdownTimedOut.Token);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Failed to dispatch message {MessageType}.", message.GetType().Name);
        }
        finally
        {
            _inFlight = 0;
        }
    }
}
