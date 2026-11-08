using PSuite.Shared.Abstractions.Messaging;

namespace PSuite.Shared.Abstractions.Commands;

public interface ICommand : IMessage;

public interface ICommand<TResult> : IMessage;