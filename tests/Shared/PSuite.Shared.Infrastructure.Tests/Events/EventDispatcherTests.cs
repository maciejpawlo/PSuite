using System.Collections.Concurrent;
using Microsoft.Extensions.DependencyInjection;
using PSuite.Shared.Abstractions.Events;
using PSuite.Shared.Infrastructure.Events;

namespace PSuite.Shared.Infrastructure.Tests.Events;

public class EventDispatcherTests : IDisposable
{
    private readonly Recorder _recorder = new();
    private readonly EventLog _log = new();
    private readonly ServiceProvider _root;
    private readonly IServiceScope _scope;
    private readonly IEventDispatcher _dispatcher;

    public EventDispatcherTests()
    {
        _root = TestProvider.Build(_recorder, services =>
        {
            services.AddSingleton(_log);
            services.AddEvents(TestProvider.Assemblies);
        });
        _scope = _root.CreateScope();
        _dispatcher = _scope.ServiceProvider.GetRequiredService<IEventDispatcher>();
    }

    [Fact]
    public async Task given_two_handlers_should_share_one_scoped_dependency()
    {
        await _dispatcher.PublishAsync(new TwoHandlersEvent());

        _log.Probes.Count.ShouldBe(2);
        ReferenceEquals(_log.Probes.First(), _log.Probes.Last()).ShouldBeTrue();
    }

    [Fact]
    public async Task given_internal_handler_should_invoke_it()
    {
        await _dispatcher.PublishAsync(new InternalEvent());

        _recorder.Handled.ShouldBeTrue();
    }

    [Fact]
    public async Task given_failing_handler_other_handlers_should_still_run()
    {
        await Should.ThrowAsync<TestDomainException>(() => _dispatcher.PublishAsync(new PartiallyFailingEvent()));

        _recorder.Handled.ShouldBeTrue();
    }

    [Fact]
    public async Task given_no_handlers_should_complete()
    {
        await Should.NotThrowAsync(() => _dispatcher.PublishAsync(new UnhandledEvent()));
    }

    [Fact]
    public async Task given_async_handler_scope_should_stay_open_until_it_finishes()
    {
        await _dispatcher.PublishAsync(new SlowEvent());

        _recorder.DisposedDuringHandler.ShouldBe(false);
        _recorder.Probe!.Disposed.ShouldBeTrue();
    }

    [Fact]
    public void dispatcher_should_resolve_from_root_provider()
    {
        _root.GetRequiredService<IEventDispatcher>().ShouldNotBeNull();
    }

    public void Dispose()
    {
        _scope.Dispose();
        _root.Dispose();
    }
}

public sealed class EventLog
{
    public ConcurrentBag<ScopedProbe> Probes { get; } = [];
}

public sealed record TwoHandlersEvent : IEvent;

public sealed class FirstTwoHandlersEventHandler(ScopedProbe probe, EventLog log) : IEventHandler<TwoHandlersEvent>
{
    public async Task HandleAsync(TwoHandlersEvent @event, CancellationToken cancellationToken = default)
    {
        log.Probes.Add(probe);
        await Task.Yield();
    }
}

public sealed class SecondTwoHandlersEventHandler(ScopedProbe probe, EventLog log) : IEventHandler<TwoHandlersEvent>
{
    public async Task HandleAsync(TwoHandlersEvent @event, CancellationToken cancellationToken = default)
    {
        log.Probes.Add(probe);
        await Task.Yield();
    }
}

internal sealed record InternalEvent : IEvent;

internal sealed class InternalEventHandler(Recorder recorder) : IEventHandler<InternalEvent>
{
    public Task HandleAsync(InternalEvent @event, CancellationToken cancellationToken = default)
    {
        recorder.Handled = true;
        return Task.CompletedTask;
    }
}

public sealed record PartiallyFailingEvent : IEvent;

public sealed class FailingEventHandler : IEventHandler<PartiallyFailingEvent>
{
    public Task HandleAsync(PartiallyFailingEvent @event, CancellationToken cancellationToken = default)
        => throw new TestDomainException();
}

public sealed class SurvivingEventHandler(Recorder recorder) : IEventHandler<PartiallyFailingEvent>
{
    public Task HandleAsync(PartiallyFailingEvent @event, CancellationToken cancellationToken = default)
    {
        recorder.Handled = true;
        return Task.CompletedTask;
    }
}

public sealed record UnhandledEvent : IEvent;

public sealed record SlowEvent : IEvent;

public sealed class SlowEventHandler(ScopedProbe probe, Recorder recorder) : IEventHandler<SlowEvent>
{
    public async Task HandleAsync(SlowEvent @event, CancellationToken cancellationToken = default)
    {
        recorder.Probe = probe;
        await Task.Delay(20, CancellationToken.None);
        recorder.DisposedDuringHandler = probe.Disposed;
    }
}
