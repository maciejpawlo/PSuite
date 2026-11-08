using PSuite.Shared.Abstractions.Modules;

namespace PSuite.Shared.Infrastructure.Modules;

internal sealed class ModuleClient(IModuleRegistry moduleRegistry, IModuleSerializer moduleSerializer) : IModuleClient
{
    public Task SendAsync(string path, object request, CancellationToken cancellationToken = default) 
        => SendAsync<object>(path, request, cancellationToken);

    // NOTE: we need to translate request to upstream type
    // and response to downstream
    public async Task<TResult> SendAsync<TResult>(string path, object request, CancellationToken cancellationToken = default) where TResult : class
    {
        var requestRegistration = moduleRegistry.GetRequestRegistration(path) 
            ?? throw new InvalidOperationException($"Module request registration for {path} not found.");
        
        var result = await requestRegistration.Action(TranslateType(request, requestRegistration.RequestType), cancellationToken);
        
        return result is null ? null! : TranslateType<TResult>(result);
    }

    public Task PublishAsync(object message, CancellationToken cancellationToken = default)
    {
        var messageType = message.GetType();
        var broadcastRegistration = moduleRegistry
            .GetBroadcastRegistrations(messageType.Name)
            .Where(x => x.ReceiverType != messageType);

        var tasks = broadcastRegistration
            .Select(async x =>
            {
                // Inside the async lambda, so one receiver's translation failure stays in its own task.
                var receiverMessage = TranslateType(message, x.ReceiverType);
                await x.Action(receiverMessage, cancellationToken);
            })
            .ToArray();
        
        return Task.WhenAll(tasks);
    }
    
    private T TranslateType<T>(object value)
        => moduleSerializer.Deserialize<T>(moduleSerializer.Serialize(value));
        
    private object TranslateType(object value, Type type)
        => moduleSerializer.Deserialize(moduleSerializer.Serialize(value), type);
}