using PSuite.Modules.Configuration.Core.DAL;
using PSuite.Modules.Configuration.Core.Entities;
using PSuite.Modules.Configuration.Core.Events;
using PSuite.Shared.Abstractions.Commands;
using PSuite.Shared.Abstractions.Messaging;

namespace PSuite.Modules.Configuration.Core.Commands.Handlers;

internal sealed class CreateHotelHandler(ConfigurationDbContext dbContext, IMessageBroker messageBroker)
    : ICommandHandler<CreateHotel, Guid>
{
    public async Task<Guid> HandleAsync(CreateHotel command, CancellationToken cancellationToken = default)
    {
        var hotel = new Hotel(command.Name);
        dbContext.Hotels.Add(hotel);
        await dbContext.SaveChangesAsync(cancellationToken);

        // Already committed: a client disconnect must not lose the event.
        await messageBroker.PublishAsync(new HotelCreated(hotel.Id, hotel.Name), CancellationToken.None);

        return hotel.Id;
    }
}
