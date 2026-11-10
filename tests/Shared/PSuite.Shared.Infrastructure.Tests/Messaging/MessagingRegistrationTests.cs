using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using PSuite.Shared.Abstractions.Events;
using PSuite.Shared.Abstractions.Messaging;
using PSuite.Shared.Infrastructure.Commands;
using PSuite.Shared.Infrastructure.Events;
using PSuite.Shared.Infrastructure.Messaging;
using PSuite.Shared.Infrastructure.Messaging.Dispatchers;
using PSuite.Shared.Infrastructure.Modules;

namespace PSuite.Shared.Infrastructure.Tests.Messaging
{
    public class MessagingRegistrationTests
    {
        private static IServiceCollection AddConfiguration(IServiceCollection services, Dictionary<string, string?> values)
            => services.AddSingleton<IConfiguration>(new ConfigurationBuilder().AddInMemoryCollection(values).Build());

        private static Dictionary<string, string?> Enabled => new() { ["messaging:useBackgroundDispatcher"] = "true" };

        [Fact]
        public void given_enabled_should_register_hosted_dispatcher()
        {
            var services = AddConfiguration(new ServiceCollection(), Enabled);

            services.AddMessaging();

            services.ShouldContain(descriptor => descriptor.ImplementationType == typeof(BackgroundDispatcher));
        }

        [Fact]
        public void given_missing_section_should_not_register_hosted_dispatcher()
        {
            var services = AddConfiguration(new ServiceCollection(), []);

            services.AddMessaging();

            services.ShouldNotContain(descriptor => descriptor.ImplementationType == typeof(BackgroundDispatcher));
        }

        [Fact]
        public async Task published_event_should_reach_other_module_handler_through_channel()
        {
            var delivery = new DeliveryProbe();
            await using var root = TestProvider.Build(new Recorder(), services =>
            {
                AddConfiguration(services, Enabled);
                services.AddLogging();
                services.AddSingleton(delivery);
                services.AddCommands(TestProvider.Assemblies);
                services.AddEvents(TestProvider.Assemblies);
                services.AddModuleRequests(TestProvider.Assemblies);
                services.AddMessaging();
            });
            var hosted = root.GetServices<IHostedService>().Single();
            await hosted.StartAsync(CancellationToken.None);
            var id = Guid.NewGuid();

            await root.GetRequiredService<IMessageBroker>().PublishAsync(new Publisher.OrderShipped(id));

            (await delivery.Received.Task.WaitAsync(TimeSpan.FromSeconds(5))).ShouldBe(id);
            await hosted.StopAsync(CancellationToken.None);
        }
    }

    public sealed class DeliveryProbe
    {
        public TaskCompletionSource<Guid> Received { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
    }
}

namespace PSuite.Shared.Infrastructure.Tests.Messaging.Publisher
{
    public sealed record OrderShipped(Guid Id) : IEvent;
}

namespace PSuite.Shared.Infrastructure.Tests.Messaging.Receiver
{
    public sealed record OrderShipped(Guid Id) : IEvent;

    public sealed class OrderShippedHandler(DeliveryProbe delivery) : IEventHandler<OrderShipped>
    {
        public Task HandleAsync(OrderShipped @event, CancellationToken cancellationToken = default)
        {
            delivery.Received.TrySetResult(@event.Id);
            return Task.CompletedTask;
        }
    }
}
