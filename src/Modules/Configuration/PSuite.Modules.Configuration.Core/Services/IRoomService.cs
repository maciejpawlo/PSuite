using PSuite.Modules.Configuration.Core.DTO;

namespace PSuite.Modules.Configuration.Core.Services;

internal interface IRoomService
{
    Task CreateAsync(RoomDto room);
    Task DeleteAsync(Guid id);
    Task UpdateAsync(RoomDto room);
    Task<IEnumerable<RoomDto>> GetAllAsync();
    Task<RoomDto> GetByIdAsync(Guid id);
}
