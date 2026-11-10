using NSubstitute;
using PSuite.Shared.Abstractions.Messaging;
using PSuite.Shared.Abstractions.Modules;
using PSuite.Shared.Infrastructure.Messaging.Brokers;
using PSuite.Shared.Infrastructure.Messaging.Dispatchers;

namespace PSuite.Shared.Infrastructure.Tests.Messaging;

public class MessageBrokerTests
{
    private readonly IModuleClient _moduleClient = Substitute.For<IModuleClient>();
    private readonly TestMessage _message = new(1);

    [Fact]
    public async Task given_background_disabled_should_publish_through_module_client()
    {
        var (broker, _) = CreateBroker(new MessagingOptions { UseBackgroundDispatcher = false });
        using var cts = new CancellationTokenSource();

        await broker.PublishAsync(_message, cts.Token);

        await _moduleClient.Received(1).PublishAsync(_message, cts.Token);
    }

    [Fact]
    public async Task given_background_enabled_should_write_to_channel_only()
    {
        var (broker, channel) = CreateBroker(new MessagingOptions { UseBackgroundDispatcher = true });

        await broker.PublishAsync(_message);

        channel.Reader.TryRead(out var written).ShouldBeTrue();
        written.ShouldBe(_message);
        await _moduleClient.DidNotReceiveWithAnyArgs().PublishAsync(default!, default);
    }

    [Fact]
    public async Task given_null_or_empty_messages_should_do_nothing()
    {
        var (broker, _) = CreateBroker(new MessagingOptions());

        await broker.PublishAsync((IMessage)null!);
        await broker.PublishAsync((IMessage[])null!);
        await broker.PublishAsync(Array.Empty<IMessage>());

        await _moduleClient.DidNotReceiveWithAnyArgs().PublishAsync(default!, default);
    }

    [Fact]
    public async Task given_array_with_null_should_publish_remaining()
    {
        var (broker, _) = CreateBroker(new MessagingOptions());

        await broker.PublishAsync([null!, _message]);

        await _moduleClient.Received(1).PublishAsync(Arg.Any<object>(), Arg.Any<CancellationToken>());
        await _moduleClient.Received(1).PublishAsync(_message, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task given_background_enabled_publishes_should_not_wait_for_the_reader()
    {
        var (broker, channel) = CreateBroker(new MessagingOptions { UseBackgroundDispatcher = true });

        for (var i = 0; i < 2000; i++)
        {
            await broker.PublishAsync(new TestMessage(i)).WaitAsync(TimeSpan.FromSeconds(5));
        }

        channel.Reader.Count.ShouldBe(2000);
    }

    private (MessageBroker Broker, MessageChannel Channel) CreateBroker(MessagingOptions options)
    {
        var channel = new MessageChannel();
        return (new MessageBroker(_moduleClient, new AsyncMessageDispatcher(channel), options), channel);
    }
}

public sealed record TestMessage(int Value) : IMessage;
