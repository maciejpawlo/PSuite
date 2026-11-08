namespace PSuite.Shared.Infrastructure.Messaging.Dispatchers;

/// <summary>Settings of the in-memory messaging, bound from the "messaging" configuration section.</summary>
public sealed class MessagingOptions
{
    /// <summary>When true, messages go through the channel and are delivered in the background.</summary>
    public bool UseBackgroundDispatcher { get; set; }

    /// <summary>Maximum number of queued messages; publishers wait when the channel is full.</summary>
    public int Capacity { get; set; } = 1000;
}
