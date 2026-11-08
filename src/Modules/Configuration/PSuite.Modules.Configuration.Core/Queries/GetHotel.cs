using PSuite.Modules.Configuration.Core.DTO;
using PSuite.Shared.Abstractions.Queries;

namespace PSuite.Modules.Configuration.Core.Queries;

internal sealed record GetHotel(Guid Id) : IQuery<HotelDetailsDto>;
