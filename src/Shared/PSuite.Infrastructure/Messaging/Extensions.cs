using Microsoft.Extensions.DependencyInjection;
using PSuite.Shared.Abstractions.Messaging;
using PSuite.Shared.Infrastructure.Messaging.Brokers;
using PSuite.Shared.Infrastructure.Messaging.Dispatchers;

namespace PSuite.Shared.Infrastructure.Messaging;

internal static class Extensions
{
    private const string SectionName = "messaging";

    internal static IServiceCollection AddMessaging(this IServiceCollection services)
    {
        var messagingOptions = services.GetOptions<MessagingOptions>(SectionName);

        services.AddSingleton(messagingOptions);
        services.AddSingleton<IMessageBroker, MessageBroker>();
        services.AddSingleton<IMessageChannel, MessageChannel>();
        services.AddSingleton<IAsyncMessageDispatcher, AsyncMessageDispatcher>();

        if (messagingOptions.UseBackgroundDispatcher)
        {
            services.AddHostedService<BackgroundDispatcher>();
        }

        return services;
    }
}
