using PSuite.Modules.Configuration.Core.DTO;
using PSuite.Modules.Configuration.Core.Entities;

namespace PSuite.Modules.Configuration.Core.Mappers;

internal static class EmployeeMapper
{
    internal static EmployeeDto ToDto(this Employee employee) => new (employee.Id, employee.FirstName, employee.LastName, employee.Hotel?.Id);
}
