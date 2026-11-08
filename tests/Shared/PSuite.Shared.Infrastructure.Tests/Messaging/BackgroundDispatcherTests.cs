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
    private readonly MessageChannel _channel = new(new MessagingOptions());

    private BackgroundDispatcher CreateDispatcher()
        => new(_channel, _moduleClient, NullLogger<BackgroundDispatcher>.Instance);

    [Fact]
    public async Task given_handler_publishing_into_full_channel_should_not_deadlock()
    {
        var channel = new MessageChannel(new MessagingOptions { Capacity = 1 });
        var publisher = new AsyncMessageDispatcher(channel);
        var lastDelivered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        _moduleClient.PublishAsync(default!, default)
            .ReturnsForAnyArgs(async call =>
            {
                var message = (TestMessage)call.ArgAt<object>(0);
                if (message.Value == 1)
                {
                    await publisher.PublishAsync(new TestMessage(2));
                    await publisher.PublishAsync(new TestMessage(3));
                }

                if (message.Value == 3)
                {
                    lastDelivered.TrySetResult();
                }
            });
        var dispatcher = new BackgroundDispatcher(channel, _moduleClient, NullLogger<BackgroundDispatcher>.Instance);
        await dispatcher.StartAsync(CancellationToken.None);

        await channel.Writer.WriteAsync(new TestMessage(1));

        await lastDelivered.Task.WaitAsync(TimeSpan.FromSeconds(5));
        await dispatcher.StopAsync(CancellationToken.None).WaitAsync(TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task given_handler_publishing_during_drain_should_deliver_follow_up()
    {
        var publisher = new AsyncMessageDispatcher(_channel);
        var delivered = new ConcurrentQueue<int>();
        var firstStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var stopRequested = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        _moduleClient.PublishAsync(default!, default)
            .ReturnsForAnyArgs(async call =>
            {
                var message = (TestMessage)call.ArgAt<object>(0);
                delivered.Enqueue(message.Value);
                if (message.Value == 1)
                {
                    firstStarted.TrySetResult();
                    await stopRequested.Task;
                    await publisher.PublishAsync(new TestMessage(2));
                }
            });
        var dispatcher = CreateDispatcher();
        await dispatcher.StartAsync(CancellationToken.None);
        await _channel.Writer.WriteAsync(new TestMessage(1));
        await firstStarted.Task.WaitAsync(TimeSpan.FromSeconds(5));

        var stopping = dispatcher.StopAsync(CancellationToken.None);
        stopRequested.TrySetResult();
        await stopping.WaitAsync(TimeSpan.FromSeconds(5));

        delivered.ToArray().ShouldBe([1, 2]);
    }

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
        logger.Entries.ShouldContain(entry => entry.Level == LogLevel.Warning && entry.Message.Contains("2"));
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
