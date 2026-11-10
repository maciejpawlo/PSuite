using PSuite.Shared.Abstractions.Commands;

namespace PSuite.Modules.Configuration.Core.Commands;

internal sealed record DeleteHotel(Guid Id) : ICommand;
