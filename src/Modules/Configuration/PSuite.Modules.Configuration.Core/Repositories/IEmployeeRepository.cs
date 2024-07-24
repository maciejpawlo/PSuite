using PSuite.Modules.Configuration.Core.Entities;

namespace PSuite.Modules.Configuration.Core.Repositories;

internal interface IEmployeeRepository
{
    Task CreateAsync(Employee employee);
    Task DeleteAsync(Employee employee);
    Task UpdateAsync(Employee employee);
    Task<IEnumerable<Employee>> GetAllAsync();
    Task<Employee> GetByIdAsync(Guid id);
}
