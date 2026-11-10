using PSuite.Shared.Abstractions.Events;

namespace PSuite.Modules.Configuration.Core.Events;

internal sealed record HotelCreated(Guid Id, string? Name) : IEvent;
