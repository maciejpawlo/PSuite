using PSuite.Shared.Abstractions.Exceptions;

namespace PSuite.Modules.Configuration.Core.Exceptions;

internal class HotelNotFoundException(Guid hotelId) : PSuiteException($"Could not find hotel with id: {hotelId}", ExceptionCategory.NotFound)
{

}
