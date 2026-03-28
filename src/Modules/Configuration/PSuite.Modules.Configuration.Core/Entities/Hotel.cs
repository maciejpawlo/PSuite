namespace PSuite.Modules.Configuration.Core.Entities;

internal class Hotel
{
    public Guid Id { get; init; }
    public string? Name { get; set; }
    public IList<Room> Rooms { get; set; } = [];
    public IList<Employee> Employees { get; set; } = [];

    private Hotel() { }

    public Hotel(string? name)
    {
        Id = Guid.CreateVersion7();
        Name = name;
    }

    public bool CanBeDeleted() 
        => !Rooms.Any() && !Employees.Any();
}
