using PSuite.Modules.Configuration.Core.DTO;
using PSuite.Modules.Configuration.Core.Entities;
using PSuite.Modules.Configuration.Core.Exceptions;
using PSuite.Modules.Configuration.Core.Mappers;
using PSuite.Modules.Configuration.Core.Repositories;

namespace PSuite.Modules.Configuration.Core.Services;

internal sealed class RoomService(IRoomRepository roomRepository, IHotelRepository hotelRepository) : IRoomService
{
    private readonly IRoomRepository roomRepository = roomRepository;
    private readonly IHotelRepository hotelRepository = hotelRepository;

    public async Task CreateAsync(RoomDto dto)
    {
        var hotel = await hotelRepository.GetByIdAsync(dto.HotelId) ?? throw new HotelNotFoundException(dto.HotelId);
        var room = new Room
        {
            Id = Guid.NewGuid(),
            Capacity = dto.Capacity,
            Hotel = hotel,
            Number = dto.Number
        };

        await roomRepository.CreateAsync(room);
    }

    public async Task DeleteAsync(Guid id)
    {
        var room = await roomRepository.GetByIdAsync(id) ?? throw new HotelNotFoundException(id);
        await roomRepository.DeleteAsync(room);
    }

    public async Task<IEnumerable<RoomDto>> GetAllAsync()
    {
        var rooms = await roomRepository.GetAllAsync();
        return rooms
            .Select(x => x.ToDto())
            .ToArray();
    }

    public async Task<RoomDto> GetByIdAsync(Guid id)
    {
        var room = await roomRepository.GetByIdAsync(id) ?? throw new RoomNotFoundException(id);
        return room.ToDto();
    }

    public async Task UpdateAsync(RoomDto dto)
    {
        var room = await roomRepository.GetByIdAsync(dto.Id) ?? throw new RoomNotFoundException(dto.Id);
        room.Number = dto.Number;
        room.Capacity = dto.Capacity;        
        await roomRepository.UpdateAsync(room);
    }
}
