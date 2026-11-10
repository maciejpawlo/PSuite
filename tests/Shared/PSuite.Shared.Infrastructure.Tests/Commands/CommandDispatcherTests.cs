using Microsoft.Extensions.DependencyInjection;
using PSuite.Shared.Abstractions.Commands;
using PSuite.Shared.Infrastructure.Commands;

namespace PSuite.Shared.Infrastructure.Tests.Commands;

public class CommandDispatcherTests : IDisposable
{
    private readonly Recorder _recorder = new();
    private readonly ServiceProvider _root;
    private readonly IServiceScope _scope;
    private readonly ICommandDispatcher _dispatcher;

    public CommandDispatcherTests()
    {
        _root = TestProvider.Build(_recorder, services => services.AddCommands(TestProvider.Assemblies));
        _scope = _root.CreateScope();
        _dispatcher = _scope.ServiceProvider.GetRequiredService<ICommandDispatcher>();
    }

    [Fact]
    public async Task given_internal_handler_should_invoke_it()
    {
        await _dispatcher.SendAsync(new InternalCommand());

        _recorder.Handled.ShouldBeTrue();
    }

    [Fact]
    public async Task given_async_handler_scope_should_stay_open_until_it_finishes()
    {
        await _dispatcher.SendAsync(new SlowCommand());

        _recorder.DisposedDuringHandler.ShouldBe(false);
        _recorder.Probe!.Disposed.ShouldBeTrue();
    }

    [Fact]
    public async Task given_cancellation_token_should_pass_it_to_handler()
    {
        using var cts = new CancellationTokenSource();

        await _dispatcher.SendAsync(new SlowCommand(), cts.Token);

        _recorder.Token.ShouldBe(cts.Token);
    }

    [Fact]
    public async Task given_missing_handler_should_throw()
    {
        await Should.ThrowAsync<InvalidOperationException>(() => _dispatcher.SendAsync(new NoHandlerCommand()));
    }

    [Fact]
    public void dispatcher_should_resolve_from_root_provider()
    {
        _root.GetRequiredService<ICommandDispatcher>().ShouldNotBeNull();
    }

    [Fact]
    public async Task given_result_command_inferred_overload_should_return_result()
    {
        var id = Guid.NewGuid();

        Guid result = await _dispatcher.SendAsync(new CreateThing(id));

        result.ShouldBe(id);
    }

    [Fact]
    public async Task given_result_command_typed_as_interface_should_return_result()
    {
        var id = Guid.NewGuid();
        ICommand<Guid> command = new CreateThing(id);

        var result = await _dispatcher.SendAsync(command);

        result.ShouldBe(id);
    }

    [Fact]
    public async Task given_result_command_should_pass_cancellation_token_to_handler()
    {
        using var cts = new CancellationTokenSource();

        await _dispatcher.SendAsync(new CreateThing(Guid.NewGuid()), cts.Token);

        _recorder.Token.ShouldBe(cts.Token);
    }

    [Fact]
    public async Task given_result_command_scope_should_stay_open_until_handler_finishes()
    {
        await _dispatcher.SendAsync(new SlowResultCommand());

        _recorder.DisposedDuringHandler.ShouldBe(false);
        _recorder.Probe!.Disposed.ShouldBeTrue();
    }

    [Fact]
    public async Task given_result_handler_throwing_synchronously_should_surface_original_exception()
    {
        await Should.ThrowAsync<TestDomainException>(() => _dispatcher.SendAsync(new ThrowingResultCommand()));
    }

    [Fact]
    public async Task given_result_command_without_handler_should_throw()
    {
        await Should.ThrowAsync<InvalidOperationException>(() => _dispatcher.SendAsync(new NoHandlerResultCommand()));
    }

    public void Dispose()
    {
        _scope.Dispose();
        _root.Dispose();
    }
}

internal sealed record InternalCommand : ICommand;

internal sealed class InternalCommandHandler(Recorder recorder) : ICommandHandler<InternalCommand>
{
    public Task HandleAsync(InternalCommand command, CancellationToken cancellationToken = default)
    {
        recorder.Handled = true;
        return Task.CompletedTask;
    }
}

public sealed record SlowCommand : ICommand;

public sealed class SlowCommandHandler(ScopedProbe probe, Recorder recorder) : ICommandHandler<SlowCommand>
{
    public async Task HandleAsync(SlowCommand command, CancellationToken cancellationToken = default)
    {
        recorder.Probe = probe;
        recorder.Token = cancellationToken;
        await Task.Delay(20, CancellationToken.None);
        recorder.DisposedDuringHandler = probe.Disposed;
    }
}

public sealed record NoHandlerCommand : ICommand;

public sealed record CreateThing(Guid Id) : ICommand<Guid>;

public sealed class CreateThingHandler(Recorder recorder) : ICommandHandler<CreateThing, Guid>
{
    public Task<Guid> HandleAsync(CreateThing command, CancellationToken cancellationToken = default)
    {
        recorder.Token = cancellationToken;
        return Task.FromResult(command.Id);
    }
}

public sealed record SlowResultCommand : ICommand<int>;

public sealed class SlowResultCommandHandler(ScopedProbe probe, Recorder recorder)
    : ICommandHandler<SlowResultCommand, int>
{
    public async Task<int> HandleAsync(SlowResultCommand command, CancellationToken cancellationToken = default)
    {
        recorder.Probe = probe;
        await Task.Delay(20, CancellationToken.None);
        recorder.DisposedDuringHandler = probe.Disposed;
        return 1;
    }
}

public sealed record ThrowingResultCommand : ICommand<int>;

public sealed class ThrowingResultCommandHandler : ICommandHandler<ThrowingResultCommand, int>
{
    public Task<int> HandleAsync(ThrowingResultCommand command, CancellationToken cancellationToken = default)
        => throw new TestDomainException();
}

public sealed record NoHandlerResultCommand : ICommand<int>;
