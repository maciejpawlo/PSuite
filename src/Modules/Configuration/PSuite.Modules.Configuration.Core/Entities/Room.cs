namespace PSuite.Modules.Configuration.Core.Entities;

internal class Room
{
    public Guid Id { get; set; }
    public int Capacity { get; set; }
    public required string Number { get; set; }
    public required Hotel Hotel { get; set; }
}
