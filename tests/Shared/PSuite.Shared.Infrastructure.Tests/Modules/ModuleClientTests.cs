using System.Text.Json;
using NSubstitute;
using PSuite.Shared.Infrastructure.Modules;

namespace PSuite.Shared.Infrastructure.Tests.Modules;

public class ModuleClientTests
{
    private sealed record Published(Guid Id);
    private sealed record FirstReceiver(Guid Id);
    private sealed record DriftedReceiver(int Id);
    private sealed record LastReceiver(Guid Id);

    [Fact]
    public async Task given_translation_failure_for_one_receiver_other_receivers_should_still_be_called()
    {
        var invoked = new List<Type>();
        var registry = Substitute.For<IModuleRegistry>();
        registry.GetBroadcastRegistrations(Arg.Any<string>()).Returns([
            Registration(typeof(FirstReceiver), invoked),
            Registration(typeof(DriftedReceiver), invoked),
            Registration(typeof(LastReceiver), invoked)
        ]);
        var client = new ModuleClient(registry, new JsonModuleSerializer());

        await Should.ThrowAsync<JsonException>(() => client.PublishAsync(new Published(Guid.NewGuid())));

        invoked.ShouldBe([typeof(FirstReceiver), typeof(LastReceiver)]);
    }

    private static ModuleBroadcastRegistration Registration(Type receiverType, List<Type> invoked)
        => new(receiverType, (_, _) =>
        {
            invoked.Add(receiverType);
            return Task.CompletedTask;
        });
}
