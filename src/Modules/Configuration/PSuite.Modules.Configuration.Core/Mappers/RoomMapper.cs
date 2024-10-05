using PSuite.Modules.Configuration.Core.DTO;
using PSuite.Modules.Configuration.Core.Entities;

namespace PSuite.Modules.Configuration.Core.Mappers;

internal static class RoomMapper
{
    internal static RoomDto ToDto(this Room room) => new (room.Id, room.Capacity, room.Number, room.Hotel.Id);
}
