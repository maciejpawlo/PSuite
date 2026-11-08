using System.Collections.Concurrent;
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
