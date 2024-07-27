namespace PSuite.Modules.Configuration.Core.DTO;

internal record HotelDetailsDto(Guid Id, string? Name, IEnumerable<RoomDto> Rooms, IEnumerable<EmployeeDto> Employees);
