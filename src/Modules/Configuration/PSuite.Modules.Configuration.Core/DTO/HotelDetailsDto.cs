namespace PSuite.Modules.Configuration.Core.DTO;

internal record HotelDetailsDto(Guid Id, string? Name, IList<RoomDto>? Rooms, IList<EmployeeDto>? Employees);
