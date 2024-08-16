using PSuite.Modules.Configuration.Core.DTO;
using PSuite.Modules.Configuration.Core.Entities;
using PSuite.Modules.Configuration.Core.Exceptions;
using PSuite.Modules.Configuration.Core.Keycloak;
using PSuite.Modules.Configuration.Core.Keycloak.Requests;
using PSuite.Modules.Configuration.Core.Mappers;
using PSuite.Modules.Configuration.Core.Repositories;

namespace PSuite.Modules.Configuration.Core.Services;

internal class EmployeeService(IEmployeeRepository employeeRepository, 
    IKeycloakService keycloakService,
    IHotelRepository hotelRepository
) : IEmployeeService
{
    private readonly IEmployeeRepository employeeRepository = employeeRepository;
    private readonly IKeycloakService keycloakService = keycloakService;
    private readonly IHotelRepository hotelRepository = hotelRepository;

    public async Task CreateAsync(EmployeeDto dto)
    {
        var hotel = dto.HotelId is not null ?
        await hotelRepository.GetByIdAsync(dto.HotelId!.Value) ?? throw new HotelNotFoundException(dto.HotelId!.Value)
        : 
        null;

        var keycloakUser = new KeycloakUser(Guid.NewGuid(), dto.FirstName, dto.LastName, $"{dto.FirstName}_{dto.LastName}", string.Empty, true, false);
        var employee = new Employee 
        {
            FirstName = dto.FirstName,
            LastName = dto.LastName,
            Hotel = hotel,
            UserId = keycloakUser.Id,
        };

        await employeeRepository.CreateAsync(employee);
        try
        {
            await keycloakService.CreateUser(keycloakUser);
        }
        catch (Exception ex)
        {
            await employeeRepository.DeleteAsync(employee);
            throw new KeycloakIntegrationException(ex);
        }
    }

    public async Task DeleteAsync(Guid id)
    {
        var employee = await employeeRepository.GetByIdAsync(id) ?? throw new EmployeeNotFoundException(id);
        
        await employeeRepository.DeleteAsync(employee);
        try
        {   
            await keycloakService.DeleteUser(id);     
        }
        catch (Exception ex)
        {
            await employeeRepository.CreateAsync(employee);
            throw new KeycloakIntegrationException(ex);
        }
    }

    public async Task<IEnumerable<EmployeeDto>> GetAllAsync()
    {
        var employees =  await employeeRepository.GetAllAsync();
        return employees.Select(x => x.ToDto()).ToList();
    }

    public async Task<EmployeeDto> GetByIdAsync(Guid id)
    {
        var employee = await employeeRepository.GetByIdAsync(id) ?? throw new EmployeeNotFoundException(id);
        return employee.ToDto();
    }

    public async Task UpdateAsync(EmployeeDto dto)
    {
        var employee = await employeeRepository.GetByIdAsync(dto.Id) ?? throw new EmployeeNotFoundException(dto.Id);
        employee.FirstName = dto.FirstName;
        employee.LastName = dto.LastName;
        var hotel = dto.HotelId is not null ?
            await hotelRepository.GetByIdAsync(dto.HotelId!.Value) ?? throw new HotelNotFoundException(dto.HotelId!.Value)
            : 
            null;
        employee.Hotel = hotel;
        var keycloakUser = new KeycloakUser(Guid.NewGuid(), dto.FirstName, dto.LastName, $"{dto.FirstName}_{dto.LastName}", string.Empty, true, false);

        await employeeRepository.UpdateAsync(employee);
        try
        {
            await keycloakService.UpdateUser(keycloakUser);
        }
        catch (Exception ex)
        {
            await employeeRepository.CreateAsync(employee);
            throw new KeycloakIntegrationException(ex);
        }
    }
}
