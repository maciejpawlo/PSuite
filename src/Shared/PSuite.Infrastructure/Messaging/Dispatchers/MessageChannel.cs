using System.Threading.Channels;
using PSuite.Shared.Abstractions.Messaging;

namespace PSuite.Shared.Infrastructure.Messaging.Dispatchers;

internal sealed class MessageChannel(MessagingOptions options) : IMessageChannel
{
    private readonly Channel<IMessage> _messages = Channel.CreateBounded<IMessage>(
        new BoundedChannelOptions(options.Capacity)
        {
            FullMode = BoundedChannelFullMode.Wait,
            SingleReader = true
        });

    public ChannelReader<IMessage> Reader => _messages.Reader;
    public ChannelWriter<IMessage> Writer => _messages.Writer;
}
