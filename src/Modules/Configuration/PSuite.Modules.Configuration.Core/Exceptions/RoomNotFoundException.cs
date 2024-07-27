using PSuite.Shared.Abstractions.Exceptions;

namespace PSuite.Modules.Configuration.Core.Exceptions;

internal class RoomNotFoundException(Guid id) : PSuiteException($"Could not find room with id: {id}", ExceptionCategory.NotFound)
{

}
