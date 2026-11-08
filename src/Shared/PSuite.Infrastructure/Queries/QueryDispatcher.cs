using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using PSuite.Shared.Abstractions.Queries;

namespace PSuite.Shared.Infrastructure.Queries;

internal sealed class QueryDispatcher(IServiceProvider serviceProvider) : IQueryDispatcher
{
    public async Task<TResult> DispatchAsync<TResult>(IQuery<TResult> query, CancellationToken cancellationToken = default)
    {
        await using var scope = serviceProvider.CreateAsyncScope();
        var handlerType = typeof(IQueryHandler<,>).MakeGenericType(query.GetType(), typeof(TResult));
        var handler = scope.ServiceProvider.GetRequiredService(handlerType);
        var method = handlerType.GetMethod(nameof(IQueryHandler<IQuery<TResult>, TResult>.HandleAsync))!;

        return await (Task<TResult>)method.Invoke(handler, BindingFlags.DoNotWrapExceptions, null, [query, cancellationToken], null)!;
    }
}
