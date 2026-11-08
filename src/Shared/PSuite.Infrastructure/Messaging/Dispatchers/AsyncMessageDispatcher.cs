using PSuite.Shared.Abstractions.Messaging;

namespace PSuite.Shared.Infrastructure.Messaging.Dispatchers;

internal sealed class AsyncMessageDispatcher(IMessageChannel messageChannel) : IAsyncMessageDispatcher
{
    public async Task PublishAsync<TMessage>(TMessage message, CancellationToken cancellationToken = default)
        where TMessage : class, IMessage
        => await messageChannel.Writer.WriteAsync(message, cancellationToken);
}
