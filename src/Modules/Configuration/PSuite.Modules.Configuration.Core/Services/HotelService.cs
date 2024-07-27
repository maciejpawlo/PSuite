using PSuite.Modules.Configuration.Core.DTO;
using PSuite.Modules.Configuration.Core.Entities;
using PSuite.Modules.Configuration.Core.Exceptions;
using PSuite.Modules.Configuration.Core.Mappers;
using PSuite.Modules.Configuration.Core.Repositories;

namespace PSuite.Modules.Configuration.Core.Services;

internal class HotelService(IHotelRepository hotelRepository) : IHotelService
{
    private readonly IHotelRepository hotelRepository = hotelRepository;

    public async Task CreateAsync(HotelDto dto)
    {
        var hotel = new Hotel
        {
            Id = dto.Id,
            Name = dto.Name,
        };
        await hotelRepository.CreateAsync(hotel);
    }

    public async Task DeleteAsync(Guid id)
    {
        var hotel = await hotelRepository.GetByIdAsync(id) ?? throw new HotelNotFoundException(id);
        if (hotel.Rooms.Any() || hotel.Employees.Any())
        {
            throw new CannotDeleteHotelException(hotel.Id);
        }
        await hotelRepository.DeleteAsync(hotel);
    }

    public async Task<IEnumerable<HotelDto>> GetAllAsync()
    {
        var hotel = await hotelRepository.GetAllAsync();
        return hotel.Select(x => new HotelDto(x.Id, x.Name)).ToList();
    }

    public async Task<HotelDetailsDto> GetByIdAsync(Guid id)
    {
        var hotel = await hotelRepository.GetByIdAsync(id) ?? throw new HotelNotFoundException(id);
        return new HotelDetailsDto(hotel.Id, hotel.Name, 
            hotel.Rooms.Select(x => x.ToDto()).ToArray(), hotel.Employees.Select(x => x.ToDto()).ToArray());
    }

    public async Task UpdateAsync(HotelDto dto)
    {
        var hotel = await hotelRepository.GetByIdAsync(dto.Id) ?? throw new HotelNotFoundException(dto.Id);
        hotel.Name = dto.Name;
        await hotelRepository.UpdateAsync(hotel);
    }
}
