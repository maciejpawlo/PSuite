using Microsoft.Extensions.DependencyInjection;
using PSuite.Shared.Abstractions.Modules;

namespace PSuite.Shared.Infrastructure.Modules;

internal sealed class ModuleSubscriber(IModuleRegistry moduleRegistry, IServiceProvider serviceProvider) : IModuleSubscriber
{
    public IModuleSubscriber Subscribe<TRequest, TResponse>(string path,
        Func<TRequest, IServiceProvider, CancellationToken, Task<TResponse>> action)
        where TRequest : class where TResponse : class
    {
        moduleRegistry.AddRequestAction(path, typeof(TRequest), typeof(TResponse),
            async (request, cancellationToken) =>
            {
                await using var scope = serviceProvider.CreateAsyncScope();
                return await action((TRequest)request, scope.ServiceProvider, cancellationToken);
            });

        return this;
    }

    public IModuleSubscriber Subscribe<TRequest>(string path,
        Func<TRequest, IServiceProvider, CancellationToken, Task> action)
        where TRequest : class
    {
        moduleRegistry.AddRequestAction(path, typeof(TRequest), typeof(object),
            async (request, cancellationToken) =>
            {
                await using var scope = serviceProvider.CreateAsyncScope();
                await action((TRequest)request, scope.ServiceProvider, cancellationToken);
                return null!;
            });

        return this;
    }
}
