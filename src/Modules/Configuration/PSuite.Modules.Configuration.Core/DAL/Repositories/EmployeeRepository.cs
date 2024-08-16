using Microsoft.EntityFrameworkCore;
using PSuite.Modules.Configuration.Core.Entities;
using PSuite.Modules.Configuration.Core.Repositories;

namespace PSuite.Modules.Configuration.Core.DAL.Repositories;

internal class EmployeeRepository(ConfigurationDbContext dbContext) : IEmployeeRepository
{
    private readonly ConfigurationDbContext dbContext = dbContext;

    public async Task CreateAsync(Employee employee)
    {
        await dbContext.Employees.AddAsync(employee);
        await dbContext.SaveChangesAsync();
    }

    public async Task DeleteAsync(Employee employee)
    {
        dbContext.Employees.Remove(employee);
        await dbContext.SaveChangesAsync();
    }

    public async Task<IEnumerable<Employee>> GetAllAsync()
        => await dbContext.Employees
            .AsNoTracking()
            .ToListAsync();

    public Task<Employee?> GetByIdAsync(Guid id)
        => dbContext.Employees.FirstOrDefaultAsync(x => x.Id == id);

    public async Task UpdateAsync(Employee employee)
    {
        dbContext.Employees.Update(employee);
        await dbContext.SaveChangesAsync();
    }
}
