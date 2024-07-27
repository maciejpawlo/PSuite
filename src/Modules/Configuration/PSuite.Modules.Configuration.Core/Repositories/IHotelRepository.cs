using PSuite.Modules.Configuration.Core.Entities;

namespace PSuite.Modules.Configuration.Core.Repositories;

internal interface IHotelRepository
{
    Task CreateAsync(Hotel hotel);
    Task DeleteAsync(Hotel hotel);
    Task UpdateAsync(Hotel hotel);
    Task<IEnumerable<Hotel>> GetAllAsync();
    Task<Hotel?> GetByIdAsync(Guid id);
}
