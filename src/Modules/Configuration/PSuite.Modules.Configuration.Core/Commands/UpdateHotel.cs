using PSuite.Shared.Abstractions.Commands;

namespace PSuite.Modules.Configuration.Core.Commands;

internal sealed record UpdateHotel(Guid Id, string? Name) : ICommand;
