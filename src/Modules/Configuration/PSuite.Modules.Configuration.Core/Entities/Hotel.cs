namespace PSuite.Modules.Configuration.Core.Entities;

internal class Hotel
{
    public Guid Id { get; set; }
    public string? Name { get; set; }
    public IList<Room> Rooms { get; set; } = [];
    public IList<Employee> Employees { get; set; } = [];
    
    public bool CanBeDeleted() 
        => !Rooms.Any() && !Employees.Any();
}
