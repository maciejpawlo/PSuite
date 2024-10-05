namespace PSuite.Modules.Configuration.Core.DTO;

public record class CreateEmployeeDto(Guid Id, string FirstName, string LastName, Guid? HotelId, string Email);
