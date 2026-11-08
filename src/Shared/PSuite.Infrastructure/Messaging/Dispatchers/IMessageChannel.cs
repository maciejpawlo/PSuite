using System.Threading.Channels;
using PSuite.Shared.Abstractions.Messaging;

namespace PSuite.Shared.Infrastructure.Messaging.Dispatchers;

/// <summary>In-memory queue between message publishers and the background dispatcher.</summary>
public interface IMessageChannel
{
    /// <summary>Reading side of the queue.</summary>
    ChannelReader<IMessage> Reader { get; }

    /// <summary>Writing side of the queue.</summary>
    ChannelWriter<IMessage> Writer { get; }
}
