using Microsoft.EntityFrameworkCore;
using PSuite.Modules.Configuration.Core.DAL;
using PSuite.Modules.Configuration.Core.DTO;
using PSuite.Modules.Configuration.Core.Entities;
using PSuite.Modules.Configuration.Core.Exceptions;
using PSuite.Modules.Configuration.Core.Mappers;

namespace PSuite.Modules.Configuration.Core.Services;

internal sealed class RoomService(ConfigurationDbContext dbContext) : IRoomService
{
    private readonly ConfigurationDbContext dbContext = dbContext;

    public async Task CreateAsync(RoomDto dto)
    {
        var hotel = await dbContext.Hotels.FirstOrDefaultAsync(x => x.Id == dto.HotelId) ?? throw new HotelNotFoundException(dto.HotelId);
        var room = new Room
        {
            Id = Guid.NewGuid(),
            Capacity = dto.Capacity,
            Hotel = hotel,
            Number = dto.Number
        };

        await dbContext.Rooms.AddAsync(room);
        await dbContext.SaveChangesAsync();
    }

    public async Task DeleteAsync(Guid id)
    {
        var room = await dbContext.Rooms.FirstOrDefaultAsync(x => x.Id == id) ?? throw new RoomNotFoundException(id);
        dbContext.Rooms.Remove(room);
        await dbContext.SaveChangesAsync();
    }

    public async Task<IEnumerable<RoomDto>> GetAllAsync()
    {
        return await dbContext.Rooms
            .AsNoTracking()
            .Include(x => x.Hotel)
            .Select(x => x.ToDto())
            .ToListAsync();
    }

    public async Task<RoomDto> GetByIdAsync(Guid id)
    {
        var room =  await dbContext.Rooms.FirstOrDefaultAsync(x => x.Id == id) ?? throw new RoomNotFoundException(id);
        return room.ToDto();
    }

    public async Task UpdateAsync(RoomDto dto)
    {
        var room = await dbContext.Rooms.FirstOrDefaultAsync(x => x.Id == dto.Id) ?? throw new RoomNotFoundException(dto.Id);
        room.Number = dto.Number;
        room.Capacity = dto.Capacity;        
        await dbContext.SaveChangesAsync();
    }
}
