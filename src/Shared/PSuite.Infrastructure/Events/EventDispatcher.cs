using Microsoft.Extensions.DependencyInjection;
using PSuite.Shared.Abstractions.Events;

namespace PSuite.Shared.Infrastructure.Events;

internal sealed class EventDispatcher(IServiceProvider serviceProvider) : IEventDispatcher
{
    public async Task PublishAsync<TEvent>(TEvent @event, CancellationToken cancellationToken = default)
        where TEvent : class, IEvent
    {
        int count;
        await using (var probe = serviceProvider.CreateAsyncScope())
        {
            count = probe.ServiceProvider.GetServices<IEventHandler<TEvent>>().Count();
        }

        // Each handler gets its own scope, so handlers running concurrently never share a DbContext.
        await Task.WhenAll(Enumerable.Range(0, count).Select(async index =>
        {
            await using var scope = serviceProvider.CreateAsyncScope();
            var handler = scope.ServiceProvider.GetServices<IEventHandler<TEvent>>().ElementAt(index);
            await handler.HandleAsync(@event, cancellationToken);
        }));
    }
}
