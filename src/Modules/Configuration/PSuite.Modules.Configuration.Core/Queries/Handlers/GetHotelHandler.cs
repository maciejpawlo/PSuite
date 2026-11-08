using Microsoft.EntityFrameworkCore;
using PSuite.Modules.Configuration.Core.DAL;
using PSuite.Modules.Configuration.Core.DTO;
using PSuite.Modules.Configuration.Core.Exceptions;
using PSuite.Modules.Configuration.Core.Mappers;
using PSuite.Shared.Abstractions.Queries;

namespace PSuite.Modules.Configuration.Core.Queries.Handlers;

internal sealed class GetHotelHandler(ConfigurationDbContext dbContext) : IQueryHandler<GetHotel, HotelDetailsDto>
{
    public async Task<HotelDetailsDto> HandleAsync(GetHotel query, CancellationToken cancellationToken = default)
    {
        var hotel = await dbContext.Hotels
            .AsNoTracking()
            .Include(x => x.Rooms)
            .Include(x => x.Employees)
            .FirstOrDefaultAsync(x => x.Id == query.Id, cancellationToken) ?? throw new HotelNotFoundException(query.Id);

        return new HotelDetailsDto(hotel.Id, hotel.Name,
            hotel.Rooms.Select(x => x.ToDto()).ToArray(), hotel.Employees.Select(x => x.ToDto()).ToArray());
    }
}
