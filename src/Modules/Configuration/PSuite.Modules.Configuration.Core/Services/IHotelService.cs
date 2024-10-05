using PSuite.Modules.Configuration.Core.DTO;

namespace PSuite.Modules.Configuration.Core.Services;

internal interface IHotelService
{
    Task CreateAsync(HotelDto hotel);
    Task DeleteAsync(Guid id);
    Task UpdateAsync(HotelDto hotel);
    Task<IEnumerable<HotelDto>> GetAllAsync();
    Task<HotelDetailsDto> GetByIdAsync(Guid id);
}
