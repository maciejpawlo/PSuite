using Microsoft.EntityFrameworkCore;
using PSuite.Modules.Configuration.Core.DAL;
using PSuite.Modules.Configuration.Core.Exceptions;
using PSuite.Shared.Abstractions.Commands;

namespace PSuite.Modules.Configuration.Core.Commands.Handlers;

internal sealed class UpdateHotelHandler(ConfigurationDbContext dbContext) : ICommandHandler<UpdateHotel>
{
    public async Task HandleAsync(UpdateHotel command, CancellationToken cancellationToken = default)
    {
        var hotel = await dbContext.Hotels
            .FirstOrDefaultAsync(x => x.Id == command.Id, cancellationToken) ?? throw new HotelNotFoundException(command.Id);

        hotel.Name = command.Name;
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
