using PSuite.Modules.Configuration.Core.DTO;
using PSuite.Modules.Configuration.Core.Entities;
using PSuite.Modules.Configuration.Core.Exceptions;
using PSuite.Modules.Configuration.Core.Keycloak;
using PSuite.Modules.Configuration.Core.Keycloak.Requests;
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

        var keycloakUser = new KeycloakUser(Guid.NewGuid(), dto.FirstName, dto.LastName, string.Empty, true, false);
        var employee = new Employee 
        {
            FirstName = dto.FirstName,
            LastName = dto.LastName,
            Hotel = hotel,
            UserId = keycloakUser.Id
        };

        try
        {
            await keycloakService.CreateUser(keycloakUser);
        }
        catch (HttpRequestException)
        {
            //TODO: throw new error related to user creation failure
        }

        await employeeRepository.CreateAsync(employee);
    }

    public Task DeleteAsync(Guid id)
    {
        throw new NotImplementedException();
    }

    public Task<IEnumerable<EmployeeDto>> GetAllAsync()
    {
        throw new NotImplementedException();
    }

    public Task<EmployeeDto> GetByIdAsync(Guid id)
    {
        throw new NotImplementedException();
    }

    public Task UpdateAsync(EmployeeDto employee)
    {
        throw new NotImplementedException();
    }
}
