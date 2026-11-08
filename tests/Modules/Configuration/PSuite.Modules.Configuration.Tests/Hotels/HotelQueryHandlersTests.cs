using PSuite.Modules.Configuration.Core.DAL;
using PSuite.Modules.Configuration.Core.Entities;
using PSuite.Modules.Configuration.Core.Exceptions;
using PSuite.Modules.Configuration.Core.Queries;
using PSuite.Modules.Configuration.Core.Queries.Handlers;

namespace PSuite.Modules.Configuration.Tests.Hotels;

public class HotelQueryHandlersTests : IDisposable
{
    private readonly ConfigurationDbContext _dbContext = TestDb.Create();

    [Fact]
    public async Task given_hotels_get_all_should_return_dtos()
    {
        var hotel = new Hotel("Grand");
        _dbContext.Hotels.Add(hotel);
        await _dbContext.SaveChangesAsync();
        _dbContext.ChangeTracker.Clear();

        var result = (await new GetHotelsHandler(_dbContext).HandleAsync(new GetHotels())).ToList();

        result.Count.ShouldBe(1);
        result[0].Id.ShouldBe(hotel.Id);
        result[0].Name.ShouldBe("Grand");
    }

    [Fact]
    public async Task given_hotel_with_rooms_get_should_return_details()
    {
        var hotel = new Hotel("Grand");
        hotel.Rooms.Add(new Room(2, "101", hotel));
        _dbContext.Hotels.Add(hotel);
        await _dbContext.SaveChangesAsync();
        _dbContext.ChangeTracker.Clear();

        var result = await new GetHotelHandler(_dbContext).HandleAsync(new GetHotel(hotel.Id));

        result.Id.ShouldBe(hotel.Id);
        result.Rooms.Single().Number.ShouldBe("101");
        result.Rooms.Single().HotelId.ShouldBe(hotel.Id);
        result.Employees.ShouldBeEmpty();
    }

    [Fact]
    public async Task given_missing_hotel_get_should_throw()
    {
        await Should.ThrowAsync<HotelNotFoundException>(
            () => new GetHotelHandler(_dbContext).HandleAsync(new GetHotel(Guid.NewGuid())));
    }

    public void Dispose() => _dbContext.Dispose();
}
