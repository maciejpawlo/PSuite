using Microsoft.EntityFrameworkCore;
using PSuite.Modules.Configuration.Core.DAL;
using PSuite.Modules.Configuration.Core.DTO;
using PSuite.Shared.Abstractions.Queries;

namespace PSuite.Modules.Configuration.Core.Queries.Handlers;

internal sealed class GetHotelsHandler(ConfigurationDbContext dbContext) : IQueryHandler<GetHotels, IEnumerable<HotelDto>>
{
    public async Task<IEnumerable<HotelDto>> HandleAsync(GetHotels query, CancellationToken cancellationToken = default)
        => await dbContext.Hotels
            .AsNoTracking()
            .Select(x => new HotelDto(x.Id, x.Name))
            .ToListAsync(cancellationToken);
}
