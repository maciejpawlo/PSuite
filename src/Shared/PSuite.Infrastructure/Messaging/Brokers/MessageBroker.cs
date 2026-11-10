using PSuite.Shared.Abstractions.Messaging;
using PSuite.Shared.Abstractions.Modules;
using PSuite.Shared.Infrastructure.Messaging.Dispatchers;

namespace PSuite.Shared.Infrastructure.Messaging.Brokers;

internal sealed class MessageBroker(
    IModuleClient moduleClient,
    IAsyncMessageDispatcher asyncMessageDispatcher,
    MessagingOptions messagingOptions) : IMessageBroker
{
    public Task PublishAsync(IMessage message, CancellationToken cancellationToken = default)
        => PublishAsync([message], cancellationToken);

    public async Task PublishAsync(IMessage[] messages, CancellationToken cancellationToken = default)
    {
        if (messages is null)
        {
            return;
        }

        var tasks = new List<Task>();

        foreach (var message in messages.Where(message => message is not null))
        {
            if (messagingOptions.UseBackgroundDispatcher)
            {
                await asyncMessageDispatcher.PublishAsync(message, cancellationToken);
                continue;
            }

            tasks.Add(moduleClient.PublishAsync(message, cancellationToken));
        }

        await Task.WhenAll(tasks);
    }
}
