using Microsoft.Extensions.DependencyInjection;
using PSuite.Shared.Abstractions.Queries;
using PSuite.Shared.Infrastructure.Queries;

namespace PSuite.Shared.Infrastructure.Tests.Queries;

public class QueryDispatcherTests : IDisposable
{
    private readonly Recorder _recorder = new();
    private readonly ServiceProvider _root;
    private readonly IServiceScope _scope;
    private readonly IQueryDispatcher _dispatcher;

    public QueryDispatcherTests()
    {
        _root = TestProvider.Build(_recorder, services => services.AddQueries(TestProvider.Assemblies));
        _scope = _root.CreateScope();
        _dispatcher = _scope.ServiceProvider.GetRequiredService<IQueryDispatcher>();
    }

    [Fact]
    public async Task given_internal_handler_should_return_result()
    {
        var result = await _dispatcher.DispatchAsync(new InternalQuery(7));

        result.ShouldBe(7);
    }

    [Fact]
    public async Task given_async_handler_scope_should_stay_open_until_it_finishes()
    {
        await _dispatcher.DispatchAsync(new SlowQuery());

        _recorder.DisposedDuringHandler.ShouldBe(false);
        _recorder.Probe!.Disposed.ShouldBeTrue();
    }

    [Fact]
    public async Task given_handler_throwing_synchronously_should_surface_original_exception()
    {
        await Should.ThrowAsync<TestDomainException>(() => _dispatcher.DispatchAsync(new ThrowingQuery()));
    }

    [Fact]
    public async Task given_missing_handler_should_throw()
    {
        await Should.ThrowAsync<InvalidOperationException>(() => _dispatcher.DispatchAsync(new NoHandlerQuery()));
    }

    [Fact]
    public void dispatcher_should_resolve_from_root_provider()
    {
        _root.GetRequiredService<IQueryDispatcher>().ShouldNotBeNull();
    }

    public void Dispose()
    {
        _scope.Dispose();
        _root.Dispose();
    }
}

internal sealed record InternalQuery(int Value) : IQuery<int>;

internal sealed class InternalQueryHandler : IQueryHandler<InternalQuery, int>
{
    public Task<int> HandleAsync(InternalQuery query, CancellationToken cancellationToken = default)
        => Task.FromResult(query.Value);
}

public sealed record SlowQuery : IQuery<int>;

public sealed class SlowQueryHandler(ScopedProbe probe, Recorder recorder) : IQueryHandler<SlowQuery, int>
{
    public async Task<int> HandleAsync(SlowQuery query, CancellationToken cancellationToken = default)
    {
        recorder.Probe = probe;
        await Task.Delay(20, CancellationToken.None);
        recorder.DisposedDuringHandler = probe.Disposed;
        return 1;
    }
}

public sealed record ThrowingQuery : IQuery<int>;

public sealed class ThrowingQueryHandler : IQueryHandler<ThrowingQuery, int>
{
    public Task<int> HandleAsync(ThrowingQuery query, CancellationToken cancellationToken = default)
        => throw new TestDomainException();
}

public sealed record NoHandlerQuery : IQuery<int>;
