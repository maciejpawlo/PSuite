using PSuite.Modules.Configuration.Core.Entities;

namespace PSuite.Modules.Configuration.Core.Repositories;

internal interface IRoomRepository
{
    Task CreateAsync(Room room);
    Task DeleteAsync(Room room);
    Task UpdateAsync(Room room);
    Task<IEnumerable<Room>> GetAllAsync();
    Task<Room?> GetByIdAsync(Guid id);
}
