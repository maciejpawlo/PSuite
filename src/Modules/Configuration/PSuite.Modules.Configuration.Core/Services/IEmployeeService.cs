using PSuite.Modules.Configuration.Core.DTO;

namespace PSuite.Modules.Configuration.Core.Services;

internal interface IEmployeeService
{
    Task CreateAsync(EmployeeDto employee);
    Task DeleteAsync(Guid id);
    Task UpdateAsync(EmployeeDto employee);
    Task<IEnumerable<EmployeeDto>> GetAllAsync();
    Task<EmployeeDto> GetByIdAsync(Guid id);
}
