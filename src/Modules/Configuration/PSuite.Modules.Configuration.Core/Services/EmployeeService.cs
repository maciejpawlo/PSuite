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
    public async Task CreateAsync(CreateEmployeeDto dto)
    {
        var hotel = await GetHotelAsync(dto.HotelId);
        
        var keycloakUser = new KeycloakUserBuilder(Guid.Empty, dto.FirstName, dto.LastName)
            .WithEmail(dto.Email)
            .Enabled()
            .Build();

        var employee = new Employee 
        {
            FirstName = dto.FirstName,
            LastName = dto.LastName,
            Hotel = hotel
        };

        dbContext.Employees.Add(employee);
        try
        {
            var externalUserId = await keycloakService.CreateUser(keycloakUser);
            employee.UserId = externalUserId;
            
            var assignRolesTask = keycloakService.AssignRealmRoles(externalUserId, Roles.Employee);
            var sendEmailTask = keycloakService.SendExecuteActionsEmail(externalUserId, RequiredActions.UpdatePassword);

            await Task.WhenAll(assignRolesTask, sendEmailTask);
        }
        catch (Exception ex)
        {
            throw new KeycloakIntegrationException(ex);
        }
        await dbContext.SaveChangesAsync();
    }

    public async Task DeleteAsync(Guid id)
    {
        var employee = await dbContext.Employees.FirstOrDefaultAsync(x => x.Id == id) 
                       ?? throw new EmployeeNotFoundException(id);
        
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

        var hotel = await GetHotelAsync(dto.HotelId);
        
        employee.FirstName = dto.FirstName;
        employee.LastName = dto.LastName;
        employee.Hotel = hotel;
        
        var keycloakUser = new KeycloakUserBuilder(employee.UserId, dto.FirstName, dto.LastName)
            .Enabled()
            .Build();        
        try
        {
            //IMPORTANT: username edition has to be enabled within realm settings
            await keycloakService.UpdateUser(keycloakUser);
        }
        catch (Exception ex)
        {
            throw new KeycloakIntegrationException(ex);
        }
        await dbContext.SaveChangesAsync();
    }
    
    private async Task<Hotel?> GetHotelAsync(Guid? hotelId)
    {
        return hotelId is not null ?
            await dbContext.Hotels.FirstOrDefaultAsync(x => x.Id == hotelId) 
                ?? throw new HotelNotFoundException(hotelId.Value)
            : 
            null;
    }
}
