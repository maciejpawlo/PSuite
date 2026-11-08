using System.Collections.Concurrent;
using PSuite.Shared.Abstractions.Messaging;

namespace PSuite.Shared.Infrastructure.Messaging.Dispatchers;

/// <summary>
/// Marks the async flow of the background dispatcher. A handler running there must not wait for channel
/// space, because only this flow can free it; its messages go to a queue drained by the same loop.
/// </summary>
internal static class DispatchContext
{
    private static readonly AsyncLocal<ConcurrentQueue<IMessage>?> Current = new();

    internal static ConcurrentQueue<IMessage>? FollowUps
    {
        get => Current.Value;
        set => Current.Value = value;
    }
}
