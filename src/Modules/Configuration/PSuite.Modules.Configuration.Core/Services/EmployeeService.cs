using Microsoft.EntityFrameworkCore;
using PSuite.Modules.Configuration.Core.DAL;
using PSuite.Modules.Configuration.Core.DTO;
using PSuite.Modules.Configuration.Core.Entities;
using PSuite.Modules.Configuration.Core.Exceptions;
using PSuite.Modules.Configuration.Core.Keycloak;
using PSuite.Modules.Configuration.Core.Keycloak.Requests;
using PSuite.Modules.Configuration.Core.Mappers;

namespace PSuite.Modules.Configuration.Core.Services;

internal class EmployeeService(ConfigurationDbContext dbContext,
    IKeycloakService keycloakService
) : IEmployeeService
{
    private readonly ConfigurationDbContext dbContext = dbContext;
    private readonly IKeycloakService keycloakService = keycloakService;

    public async Task CreateAsync(EmployeeDto dto)
    {
        var hotel = dto.HotelId is not null ?
        await dbContext.Hotels.FirstOrDefaultAsync(x => x.Id == dto.HotelId!.Value) ?? throw new HotelNotFoundException(dto.HotelId!.Value)
        : 
        null;

        var keycloakUser = new KeycloakUser(Guid.Empty, dto.FirstName, dto.LastName, $"{dto.FirstName}_{dto.LastName}", string.Empty, true, false);
        var employee = new Employee 
        {
            FirstName = dto.FirstName,
            LastName = dto.LastName,
            Hotel = hotel
        };

        await dbContext.Employees.AddAsync(employee);
        try
        {
            Guid externalUserId = await keycloakService.CreateUser(keycloakUser);
            employee.UserId = externalUserId;
        }
        catch (Exception ex)
        {
            throw new KeycloakIntegrationException(ex);
        }
        await dbContext.SaveChangesAsync();
    }

    public async Task DeleteAsync(Guid id)
    {
        var employee = await dbContext.Employees.FirstOrDefaultAsync(x => x.Id == id) ?? throw new EmployeeNotFoundException(id);
        
        dbContext.Employees.Remove(employee);
        try
        {   
            await keycloakService.DeleteUser(employee.UserId);     
        }
        catch (Exception ex)
        {
            throw new KeycloakIntegrationException(ex);
        }
        await dbContext.SaveChangesAsync();
    }

    public async Task<IEnumerable<EmployeeDto>> GetAllAsync()
    {
        var employees = dbContext.Employees
            .AsNoTracking()
            .Include(x => x.Hotel);
        return await employees.Select(x => x.ToDto()).ToListAsync();
    }

    public async Task<EmployeeDto> GetByIdAsync(Guid id)
    {
        var employee = await dbContext.Employees
            .AsNoTracking()
            .Include(x => x.Hotel)
            .FirstOrDefaultAsync(x => x.Id == id) ?? throw new EmployeeNotFoundException(id);
        return employee.ToDto();
    }

    public async Task UpdateAsync(EmployeeDto dto)
    {
        var employee = await dbContext.Employees
            .FirstOrDefaultAsync(x => x.Id == dto.Id) ?? throw new EmployeeNotFoundException(dto.Id);
        employee.FirstName = dto.FirstName;
        employee.LastName = dto.LastName;
        var hotel = dto.HotelId is not null ?
            await dbContext.Hotels.FirstOrDefaultAsync(x => x.Id == dto.HotelId) ?? throw new HotelNotFoundException(dto.HotelId!.Value)
            : 
            null;
        employee.Hotel = hotel;
        var keycloakUser = new KeycloakUser(employee.UserId, dto.FirstName, dto.LastName, $"{dto.FirstName}_{dto.LastName}", string.Empty, true, false);
        try
        {
            await keycloakService.UpdateUser(keycloakUser);
        }
        catch (Exception ex)
        {
            throw new KeycloakIntegrationException(ex);
        }
        await dbContext.SaveChangesAsync();
    }
}
