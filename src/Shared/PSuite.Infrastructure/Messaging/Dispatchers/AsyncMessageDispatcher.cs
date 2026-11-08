using PSuite.Shared.Abstractions.Messaging;

namespace PSuite.Shared.Infrastructure.Messaging.Dispatchers;

internal sealed class AsyncMessageDispatcher(IMessageChannel messageChannel) : IAsyncMessageDispatcher
{
    public async Task PublishAsync<TMessage>(TMessage message, CancellationToken cancellationToken = default)
        where TMessage : class, IMessage
    {
        if (DispatchContext.FollowUps is { } followUps)
        {
            followUps.Enqueue(message);
            return;
        }

        await messageChannel.Writer.WriteAsync(message, cancellationToken);
    }
}
