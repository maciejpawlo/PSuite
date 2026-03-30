namespace PSuite.Shared.Infrastructure.Modules;

public sealed class ModuleBroadcastRegistration
{
    public Type ReceiverType { get; } // target type in correct assembly
    public Func<object, CancellationToken, Task> Action { get; } // an action/delegate, call to message handler
    public string Key => ReceiverType.Name; // name of message type, without full namespace

    public ModuleBroadcastRegistration(Type receiverType, Func<object, CancellationToken, Task> action)
    {
        ReceiverType = receiverType;
        Action = action;
    }
}