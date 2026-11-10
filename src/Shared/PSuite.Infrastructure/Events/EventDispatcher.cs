using Microsoft.Extensions.DependencyInjection;
using PSuite.Shared.Abstractions.Events;

namespace PSuite.Shared.Infrastructure.Events;

internal sealed class EventDispatcher(IServiceProvider serviceProvider) : IEventDispatcher
{
    public async Task PublishAsync<TEvent>(TEvent @event, CancellationToken cancellationToken = default)
        where TEvent : class, IEvent
    {
        await using var scope = serviceProvider.CreateAsyncScope();
        var handlers = scope.ServiceProvider.GetServices<IEventHandler<TEvent>>();

        // Async lambda: a handler throwing synchronously must not stop the remaining handlers from running.
        await Task.WhenAll(handlers.Select(async handler => await handler.HandleAsync(@event, cancellationToken)));
    }
}
