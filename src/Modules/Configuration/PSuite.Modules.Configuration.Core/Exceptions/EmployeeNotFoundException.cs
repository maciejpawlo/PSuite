using PSuite.Shared.Abstractions.Exceptions;

namespace PSuite.Modules.Configuration.Core.Exceptions;

public class EmployeeNotFoundException(Guid employeeId) : PSuiteException($"Could not find employee with id: {employeeId}", ExceptionCategory.NotFound)
{

}
