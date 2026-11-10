using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using PSuite.Shared.Abstractions.Modules;
using PSuite.Shared.Infrastructure.Messaging.Dispatchers;

namespace PSuite.Shared.Infrastructure.Tests.Messaging;

public class BackgroundDispatcherTests
{
    private readonly IModuleClient _moduleClient = Substitute.For<IModuleClient>();
    private readonly MessageChannel _channel = new();

    private BackgroundDispatcher CreateDispatcher()
        => new(_channel, _moduleClient, NullLogger<BackgroundDispatcher>.Instance);

    [Fact]
    public async Task given_stop_timeout_with_queued_messages_should_log_undelivered_count()
    {
        var logger = new ListLogger<BackgroundDispatcher>();
        var firstStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        _moduleClient.PublishAsync(default!, default)
            .ReturnsForAnyArgs(async _ =>
            {
                firstStarted.TrySetResult();
                await release.Task;
            });
        var dispatcher = new BackgroundDispatcher(_channel, _moduleClient, logger);
        await dispatcher.StartAsync(CancellationToken.None);
        await _channel.Writer.WriteAsync(new TestMessage(1));
        await _channel.Writer.WriteAsync(new TestMessage(2));
        await _channel.Writer.WriteAsync(new TestMessage(3));
        await firstStarted.Task.WaitAsync(TimeSpan.FromSeconds(5));
        using var timeout = new CancellationTokenSource(TimeSpan.FromMilliseconds(50));

        await dispatcher.StopAsync(timeout.Token);

        release.TrySetResult();
        logger.Entries.ShouldContain(entry => entry.Level == LogLevel.Warning && entry.Message.Contains("3"));
    }

    [Fact]
    public async Task given_stop_timeout_should_cancel_in_flight_handler_and_skip_queued_messages()
    {
        var calls = 0;
        var firstStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var cancelled = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        _moduleClient.PublishAsync(default!, default)
            .ReturnsForAnyArgs(async call =>
            {
                Interlocked.Increment(ref calls);
                call.Arg<CancellationToken>().Register(() => cancelled.TrySetResult());
                firstStarted.TrySetResult();
                await Task.Delay(Timeout.Infinite, call.Arg<CancellationToken>());
            });
        var dispatcher = CreateDispatcher();
        await dispatcher.StartAsync(CancellationToken.None);
        await _channel.Writer.WriteAsync(new TestMessage(1));
        await _channel.Writer.WriteAsync(new TestMessage(2));
        await firstStarted.Task.WaitAsync(TimeSpan.FromSeconds(5));
        using var timeout = new CancellationTokenSource(TimeSpan.FromMilliseconds(50));

        await dispatcher.StopAsync(timeout.Token);

        await cancelled.Task.WaitAsync(TimeSpan.FromSeconds(5));
        await Task.Delay(100);
        calls.ShouldBe(1);
    }

    [Fact]
    public async Task given_stop_timeout_with_stuck_handler_and_empty_queue_should_log_warning()
    {
        var logger = new ListLogger<BackgroundDispatcher>();
        var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        _moduleClient.PublishAsync(default!, default)
            .ReturnsForAnyArgs(async _ =>
            {
                started.TrySetResult();
                await release.Task;
            });
        var dispatcher = new BackgroundDispatcher(_channel, _moduleClient, logger);
        await dispatcher.StartAsync(CancellationToken.None);
        await _channel.Writer.WriteAsync(new TestMessage(1));
        await started.Task.WaitAsync(TimeSpan.FromSeconds(5));
        using var timeout = new CancellationTokenSource(TimeSpan.FromMilliseconds(50));

        await dispatcher.StopAsync(timeout.Token);

        release.TrySetResult();
        logger.Entries.ShouldContain(entry => entry.Level == LogLevel.Warning && entry.Message.Contains("1"));
    }

    private sealed class ListLogger<T> : ILogger<T>
    {
        public ConcurrentBag<(LogLevel Level, string Message)> Entries { get; } = [];

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
            Func<TState, Exception?, string> formatter)
            => Entries.Add((logLevel, formatter(state, exception)));
    }

    [Fact]
    public async Task given_failing_message_should_process_next()
    {
        var failing = new TestMessage(1);
        var next = new TestMessage(2);
        var processed = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);
        _moduleClient.PublishAsync(failing, Arg.Any<CancellationToken>())
            .Returns(Task.FromException(new TestDomainException()));
        _moduleClient.PublishAsync(next, Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                processed.TrySetResult(next);
                return Task.CompletedTask;
            });
        var dispatcher = CreateDispatcher();
        await dispatcher.StartAsync(CancellationToken.None);

        await _channel.Writer.WriteAsync(failing);
        await _channel.Writer.WriteAsync(next);

        (await processed.Task.WaitAsync(TimeSpan.FromSeconds(5))).ShouldBe(next);
        await dispatcher.StopAsync(CancellationToken.None);
    }

    [Fact]
    public async Task given_stop_with_queued_messages_should_deliver_all()
    {
        var delivered = new ConcurrentBag<object>();
        var firstStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        _moduleClient.PublishAsync(default!, default)
            .ReturnsForAnyArgs(async call =>
            {
                firstStarted.TrySetResult();
                await Task.Delay(50, call.Arg<CancellationToken>());
                delivered.Add(call.Arg<object>());
            });
        var dispatcher = CreateDispatcher();
        await dispatcher.StartAsync(CancellationToken.None);
        await _channel.Writer.WriteAsync(new TestMessage(1));
        await _channel.Writer.WriteAsync(new TestMessage(2));
        await _channel.Writer.WriteAsync(new TestMessage(3));
        await firstStarted.Task.WaitAsync(TimeSpan.FromSeconds(5));

        await dispatcher.StopAsync(CancellationToken.None).WaitAsync(TimeSpan.FromSeconds(5));

        delivered.Count.ShouldBe(3);
    }
}
