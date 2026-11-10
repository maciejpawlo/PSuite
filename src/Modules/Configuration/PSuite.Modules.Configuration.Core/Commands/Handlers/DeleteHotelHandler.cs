using Microsoft.EntityFrameworkCore;
using PSuite.Modules.Configuration.Core.DAL;
using PSuite.Modules.Configuration.Core.Exceptions;
using PSuite.Shared.Abstractions.Commands;

namespace PSuite.Modules.Configuration.Core.Commands.Handlers;

internal sealed class DeleteHotelHandler(ConfigurationDbContext dbContext) : ICommandHandler<DeleteHotel>
{
    public async Task HandleAsync(DeleteHotel command, CancellationToken cancellationToken = default)
    {
        var hotel = await dbContext.Hotels
            .Include(hotel => hotel.Rooms)
            .Include(hotel => hotel.Employees)
            .FirstOrDefaultAsync(x => x.Id == command.Id, cancellationToken) ?? throw new HotelNotFoundException(command.Id);

        if (!hotel.CanBeDeleted())
        {
            throw new CannotDeleteHotelException(hotel.Id);
        }

        dbContext.Hotels.Remove(hotel);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
