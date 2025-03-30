using System.Diagnostics.CodeAnalysis;
using PSuite.Modules.Configuration.Core.Exceptions;

namespace PSuite.Modules.Configuration.Core.Entities;

internal class Room
{
    public Guid Id { get; init; }
    public int Capacity { get; set; }
    public required string Number { get; set; }
    public required Hotel Hotel { get; set; }

    [SetsRequiredMembers]
    public Room(int capacity, string number, Hotel hotel)
    {
        if (capacity < 0)
            throw new InvalidRoomCapacityException();
        
        Id = Guid.NewGuid();
        Capacity = capacity;
        Number = number;
        Hotel = hotel;
    }
}
