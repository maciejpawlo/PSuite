using PSuite.Shared.Abstractions.Exceptions;

namespace PSuite.Modules.Configuration.Core.Exceptions;

internal class CannotDeleteHotelException(Guid id) : PSuiteException($"Cannot delete hotel with id: {id}", ExceptionCategory.ValidationError)
{

}
