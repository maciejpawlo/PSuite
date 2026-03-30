using Microsoft.Extensions.DependencyInjection;
using PSuite.Shared.Abstractions.Commands;

namespace PSuite.Shared.Infrastructure.Commands;

public class CommandDispatcher(IServiceProvider serviceProvider) : ICommandDispatcher
{
    public Task SendAsync<TCommand>(TCommand command, CancellationToken cancellationToken = default) where TCommand : class, ICommand
    {
        var scope = serviceProvider.CreateScope();
        var handler = scope.ServiceProvider.GetService<ICommandHandler<TCommand>>();
        ArgumentNullException.ThrowIfNull(handler);
        return handler.HandleAsync(command, cancellationToken);
    }

    public Task<TResult> SendAsync<TResult>(ICommand<TResult> command, CancellationToken cancellationToken = default)
    {
        var scope = serviceProvider.CreateScope();
        var handlerType = typeof(ICommandHandler<,>).MakeGenericType(command.GetType(), typeof(TResult));
        var handler = scope.ServiceProvider.GetService(handlerType);
        if (handler is null)
        {
            throw new InvalidOperationException($"Handler of type {handlerType} not found.");
        }
        
        var method = handlerType.GetMethod(nameof(ICommandHandler<ICommand<TResult>, TResult>.HandleAsync));
        if (method is null)
        {
            throw new InvalidOperationException($"Command handler for '{typeof(TResult).Name}' is invalid.");
        }
        
        return (Task<TResult>)method.Invoke(handler, [command, cancellationToken])!;
    }
}