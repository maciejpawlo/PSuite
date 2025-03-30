using PSuite.Shared.Abstractions.Exceptions;

namespace PSuite.Modules.Configuration.Core.Exceptions;

internal class InvalidRoomCapacityException() 
    : PSuiteException("Room capacity cannot be lower than zero", ExceptionCategory.ValidationError)
{
    
}