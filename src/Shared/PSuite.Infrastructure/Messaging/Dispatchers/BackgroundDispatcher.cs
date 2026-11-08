using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using PSuite.Shared.Abstractions.Modules;

namespace PSuite.Shared.Infrastructure.Messaging.Dispatchers;

internal sealed class BackgroundDispatcher(
    IMessageChannel messageChannel,
    IModuleClient moduleClient,
    ILogger<BackgroundDispatcher> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Not bound to stoppingToken: on shutdown the writer is completed and the queue drains to the end.
        await foreach (var message in messageChannel.Reader.ReadAllAsync(CancellationToken.None))
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

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        messageChannel.Writer.TryComplete();
        await base.StopAsync(cancellationToken);
    }
}
