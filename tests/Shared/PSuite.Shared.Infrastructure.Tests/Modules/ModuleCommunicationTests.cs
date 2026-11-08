using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using PSuite.Shared.Abstractions.Commands;
using PSuite.Shared.Abstractions.Events;
using PSuite.Shared.Abstractions.Modules;
using PSuite.Shared.Infrastructure.Commands;
using PSuite.Shared.Infrastructure.Events;
using PSuite.Shared.Infrastructure.Modules;

namespace PSuite.Shared.Infrastructure.Tests.Modules
{
    public class ModuleCommunicationTests : IDisposable
    {
        private readonly ModuleLog _log = new();
        private readonly ServiceProvider _root;
        private readonly IModuleClient _client;
        private readonly IModuleSubscriber _subscriber;

        public ModuleCommunicationTests()
        {
            _root = TestProvider.Build(new Recorder(), services =>
            {
                services.AddSingleton(_log);
                services.AddCommands(TestProvider.Assemblies);
                services.AddEvents(TestProvider.Assemblies);
                services.AddModuleRequests(TestProvider.Assemblies);
            });
            _client = _root.GetRequiredService<IModuleClient>();
            _subscriber = _root.GetRequiredService<IModuleSubscriber>();
        }

        [Fact]
        public void registry_should_resolve_from_root_provider()
        {
            _root.GetRequiredService<IModuleRegistry>().ShouldNotBeNull();
        }

        [Fact]
        public void use_module_requests_should_return_subscriber()
        {
            new ApplicationBuilder(_root).UseModuleRequests().ShouldBeSameAs(_subscriber);
        }

        [Fact]
        public async Task given_event_should_deliver_translated_copy_to_other_module()
        {
            var id = Guid.NewGuid();

            await _client.PublishAsync(new ModuleA.ThingHappened(id));

            _log.ReceivedByB.ShouldBe(id);
        }

        [Fact]
        public async Task given_event_publisher_own_handler_should_not_be_invoked()
        {
            await _client.PublishAsync(new ModuleA.ThingHappened(Guid.NewGuid()));

            _log.ReceivedByA.ShouldBeNull();
        }

        [Fact]
        public async Task given_no_receivers_publish_should_complete()
        {
            await Should.NotThrowAsync(() => _client.PublishAsync(new NobodyListens()));
        }

        [Fact]
        public async Task given_command_should_deliver_translated_copy_to_other_module()
        {
            var id = Guid.NewGuid();

            await _client.PublishAsync(new ModuleA.DoThing(id));

            _log.ReceivedByB.ShouldBe(id);
        }

        [Fact]
        public async Task given_result_command_publish_should_not_invoke_its_handler()
        {
            await _client.PublishAsync(new ModuleA.CreateThing(Guid.NewGuid()));

            _log.ReceivedByB.ShouldBeNull();
        }

        [Fact]
        public async Task given_result_command_subscription_send_should_return_translated_result()
        {
            var id = Guid.NewGuid();
            _subscriber.Subscribe<ModuleB.CreateThing, ModuleB.ThingDto>("b/create",
                (command, sp, ct) => sp.GetRequiredService<ICommandDispatcher>().SendAsync(command, ct));

            var result = await _client.SendAsync<ModuleA.ThingDto>("b/create", new ModuleA.CreateThing(id));

            result.Id.ShouldBe(id);
        }

        [Fact]
        public async Task given_subscription_send_should_translate_request_and_response()
        {
            var id = Guid.NewGuid();
            _subscriber.Subscribe<ModuleB.GetThing, ModuleB.ThingDto>("b/thing",
                (request, sp, ct) => Task.FromResult(new ModuleB.ThingDto(request.Id)));

            var result = await _client.SendAsync<ModuleA.ThingDto>("b/thing", new ModuleA.GetThing(id));

            result.Id.ShouldBe(id);
        }

        [Fact]
        public async Task given_subscription_without_response_send_should_invoke_action()
        {
            var id = Guid.NewGuid();
            _subscriber.Subscribe<ModuleB.GetThing>("b/void", (request, sp, ct) =>
            {
                _log.ReceivedByB = request.Id;
                return Task.CompletedTask;
            });

            await _client.SendAsync("b/void", new ModuleA.GetThing(id));

            _log.ReceivedByB.ShouldBe(id);
        }

        [Fact]
        public async Task given_unknown_path_send_should_throw()
        {
            var exception = await Should.ThrowAsync<InvalidOperationException>(
                () => _client.SendAsync("b/missing", new ModuleA.GetThing(Guid.NewGuid())));

            exception.Message.ShouldContain("b/missing");
        }

        [Fact]
        public async Task given_subscription_action_scope_should_be_disposed_after_it_finishes()
        {
            ScopedProbe? probe = null;
            _subscriber.Subscribe<ModuleB.GetThing, ModuleB.ThingDto>("b/scope", async (request, sp, ct) =>
            {
                probe = sp.GetRequiredService<ScopedProbe>();
                await Task.Delay(20, CancellationToken.None);
                probe.Disposed.ShouldBeFalse();
                return new ModuleB.ThingDto(request.Id);
            });

            await _client.SendAsync<ModuleA.ThingDto>("b/scope", new ModuleA.GetThing(Guid.NewGuid()));

            probe!.Disposed.ShouldBeTrue();
        }

        public void Dispose() => _root.Dispose();
    }

    public sealed class ModuleLog
    {
        public Guid? ReceivedByA { get; set; }
        public Guid? ReceivedByB { get; set; }
    }

    public sealed record NobodyListens;
}

namespace PSuite.Shared.Infrastructure.Tests.Modules.ModuleA
{
    public sealed record ThingHappened(Guid Id) : IEvent;
    public sealed record DoThing(Guid Id) : ICommand;
    public sealed record GetThing(Guid Id);
    public sealed record CreateThing(Guid Id);
    public sealed record ThingDto(Guid Id);

    public sealed class ThingHappenedHandler(ModuleLog log) : IEventHandler<ThingHappened>
    {
        public Task HandleAsync(ThingHappened @event, CancellationToken cancellationToken = default)
        {
            log.ReceivedByA = @event.Id;
            return Task.CompletedTask;
        }
    }
}

namespace PSuite.Shared.Infrastructure.Tests.Modules.ModuleB
{
    public sealed record ThingHappened(Guid Id) : IEvent;
    public sealed record DoThing(Guid Id) : ICommand;
    public sealed record GetThing(Guid Id);
    public sealed record CreateThing(Guid Id) : ICommand<ThingDto>;
    public sealed record ThingDto(Guid Id);

    public sealed class ThingHappenedHandler(ModuleLog log) : IEventHandler<ThingHappened>
    {
        public Task HandleAsync(ThingHappened @event, CancellationToken cancellationToken = default)
        {
            log.ReceivedByB = @event.Id;
            return Task.CompletedTask;
        }
    }

    public sealed class DoThingHandler(ModuleLog log) : ICommandHandler<DoThing>
    {
        public Task HandleAsync(DoThing command, CancellationToken cancellationToken = default)
        {
            log.ReceivedByB = command.Id;
            return Task.CompletedTask;
        }
    }

    public sealed class CreateThingHandler(ModuleLog log) : ICommandHandler<CreateThing, ThingDto>
    {
        public Task<ThingDto> HandleAsync(CreateThing command, CancellationToken cancellationToken = default)
        {
            log.ReceivedByB = command.Id;
            return Task.FromResult(new ThingDto(command.Id));
        }
    }
}
