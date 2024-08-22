using Microsoft.EntityFrameworkCore;
using PSuite.Modules.Configuration.Core.DAL;
using PSuite.Modules.Configuration.Core.DTO;
using PSuite.Modules.Configuration.Core.Entities;
using PSuite.Modules.Configuration.Core.Exceptions;
using PSuite.Modules.Configuration.Core.Mappers;

namespace PSuite.Modules.Configuration.Core.Services;

internal class HotelService(ConfigurationDbContext dbContext) : IHotelService
{
    private readonly ConfigurationDbContext dbContext = dbContext;

    public async Task CreateAsync(HotelDto dto)
    {
        var hotel = new Hotel
        {
            Id = dto.Id,
            Name = dto.Name,
        };
        await dbContext.Hotels.AddAsync(hotel);
        await dbContext.SaveChangesAsync();
    }

    public async Task DeleteAsync(Guid id)
    {
        var hotel = await dbContext.Hotels.FirstOrDefaultAsync(x => x.Id == id) ?? throw new HotelNotFoundException(id);
        if (hotel.Rooms.Any() || hotel.Employees.Any())
        {
            throw new CannotDeleteHotelException(hotel.Id);
        }
        dbContext.Hotels.Remove(hotel);
        await dbContext.SaveChangesAsync();
    }

    public async Task<IEnumerable<HotelDto>> GetAllAsync()
    {
        return await dbContext.Hotels
            .AsNoTracking()
            .Select(x => new HotelDto(x.Id, x.Name))
            .ToListAsync();
    }

    public async Task<HotelDetailsDto> GetByIdAsync(Guid id)
    {
        var hotel = await dbContext.Hotels
        .AsNoTracking()
        .Include(x => x.Rooms)
        .Include(x => x.Employees)
        .FirstOrDefaultAsync(x => x.Id == id) ?? throw new HotelNotFoundException(id);
        return new HotelDetailsDto(hotel.Id, hotel.Name, 
            hotel.Rooms.Select(x => x.ToDto()).ToArray(), hotel.Employees.Select(x => x.ToDto()).ToArray());
    }

    public async Task UpdateAsync(HotelDto dto)
    {
        var hotel = await dbContext.Hotels.FirstOrDefaultAsync(x => x.Id == dto.Id) ?? throw new HotelNotFoundException(dto.Id);
        hotel.Name = dto.Name;
        await dbContext.SaveChangesAsync();
    }
}
