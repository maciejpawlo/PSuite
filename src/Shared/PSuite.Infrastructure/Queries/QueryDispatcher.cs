using Microsoft.Extensions.DependencyInjection;
using PSuite.Shared.Abstractions.Queries;

namespace PSuite.Shared.Infrastructure.Queries;

public class QueryDispatcher(IServiceProvider serviceProvider) : IQueryDispatcher
{
    public Task<TResult> DispatchAsync<TResult>(IQuery<TResult> query, CancellationToken cancellationToken = default)
    {
        var scope = serviceProvider.CreateScope();
        var handlerType = typeof(IQueryHandler<,>).MakeGenericType(query.GetType(), typeof(TResult));
        var handler = scope.ServiceProvider.GetService(handlerType);
        if (handler == null)
        {
            throw new InvalidOperationException($"Handler of type {handlerType} not found.");
        }
        
        var method = handlerType.GetMethod(nameof(IQueryHandler<IQuery<TResult>, TResult>.HandleAsync));
        if (method is null)
        {
            throw new InvalidOperationException($"Query handler for '{typeof(TResult).Name}' is invalid.");
        }
        
        return (Task<TResult>)method.Invoke(handler, [query, cancellationToken])!;
    }
}