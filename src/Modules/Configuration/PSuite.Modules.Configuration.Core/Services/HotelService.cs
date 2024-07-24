using PSuite.Modules.Configuration.Core.DTO;

namespace PSuite.Modules.Configuration.Core.Services;

internal class HotelService : IHotelService
{
    public Task CreateAsync(HotelDto hotel)
    {
        throw new NotImplementedException();
    }

    public Task DeleteAsync(Guid id)
    {
        throw new NotImplementedException();
    }

    public Task<IEnumerable<HotelDetailsDto>> GetAllAsync()
    {
        throw new NotImplementedException();
    }

    public Task<HotelDetailsDto> GetByIdAsync(Guid id)
    {
        throw new NotImplementedException();
    }

    public Task UpdateAsync(HotelDto hotel)
    {
        throw new NotImplementedException();
    }
}
