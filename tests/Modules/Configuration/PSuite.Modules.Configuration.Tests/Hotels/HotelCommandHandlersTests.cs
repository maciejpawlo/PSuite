using Microsoft.EntityFrameworkCore;
using NSubstitute;
using PSuite.Modules.Configuration.Core.Commands;
using PSuite.Modules.Configuration.Core.Commands.Handlers;
using PSuite.Modules.Configuration.Core.DAL;
using PSuite.Modules.Configuration.Core.Entities;
using PSuite.Modules.Configuration.Core.Events;
using PSuite.Modules.Configuration.Core.Exceptions;
using PSuite.Shared.Abstractions.Messaging;

namespace PSuite.Modules.Configuration.Tests.Hotels;

public class HotelCommandHandlersTests : IDisposable
{
    private readonly ConfigurationDbContext _dbContext = TestDb.Create();
    private readonly IMessageBroker _broker = Substitute.For<IMessageBroker>();

    [Fact]
    public async Task given_create_should_persist_hotel_return_id_and_publish_event()
    {
        using var cts = new CancellationTokenSource();
        var handler = new CreateHotelHandler(_dbContext, _broker);

        var id = await handler.HandleAsync(new CreateHotel("Grand"), cts.Token);

        _dbContext.ChangeTracker.Clear();
        var hotel = await _dbContext.Hotels.SingleAsync();
        hotel.Id.ShouldBe(id);
        hotel.Name.ShouldBe("Grand");
        await _broker.Received(1).PublishAsync(
            Arg.Is<IMessage>(message => message is HotelCreated && ((HotelCreated)message).Id == id),
            CancellationToken.None);
    }

    [Fact]
    public async Task given_save_failure_should_not_publish_event()
    {
        var handler = new CreateHotelHandler(_dbContext, _broker);
        await _dbContext.DisposeAsync();

        await Should.ThrowAsync<ObjectDisposedException>(() => handler.HandleAsync(new CreateHotel("Grand")));

        await _broker.DidNotReceiveWithAnyArgs().PublishAsync(default(IMessage)!, default);
    }

    [Fact]
    public async Task given_update_should_rename_hotel()
    {
        var hotel = await AddHotelAsync();

        await new UpdateHotelHandler(_dbContext).HandleAsync(new UpdateHotel(hotel.Id, "Renamed"));

        _dbContext.ChangeTracker.Clear();
        (await _dbContext.Hotels.SingleAsync()).Name.ShouldBe("Renamed");
    }

    [Fact]
    public async Task given_missing_hotel_update_should_throw()
    {
        await Should.ThrowAsync<HotelNotFoundException>(
            () => new UpdateHotelHandler(_dbContext).HandleAsync(new UpdateHotel(Guid.NewGuid(), "Renamed")));
    }

    [Fact]
    public async Task given_delete_should_remove_hotel()
    {
        var hotel = await AddHotelAsync();

        await new DeleteHotelHandler(_dbContext).HandleAsync(new DeleteHotel(hotel.Id));

        (await _dbContext.Hotels.CountAsync()).ShouldBe(0);
    }

    [Fact]
    public async Task given_hotel_with_room_delete_should_throw()
    {
        var hotel = await AddHotelAsync(withRoom: true);

        await Should.ThrowAsync<CannotDeleteHotelException>(
            () => new DeleteHotelHandler(_dbContext).HandleAsync(new DeleteHotel(hotel.Id)));
    }

    [Fact]
    public async Task given_missing_hotel_delete_should_throw()
    {
        await Should.ThrowAsync<HotelNotFoundException>(
            () => new DeleteHotelHandler(_dbContext).HandleAsync(new DeleteHotel(Guid.NewGuid())));
    }

    private async Task<Hotel> AddHotelAsync(bool withRoom = false)
    {
        var hotel = new Hotel("Grand");
        if (withRoom)
        {
            hotel.Rooms.Add(new Room(2, "101", hotel));
        }

        _dbContext.Hotels.Add(hotel);
        await _dbContext.SaveChangesAsync();
        _dbContext.ChangeTracker.Clear();
        return hotel;
    }

    public void Dispose() => _dbContext.Dispose();
}

internal static class TestDb
{
    internal static ConfigurationDbContext Create()
        => new(new DbContextOptionsBuilder<ConfigurationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);
}
