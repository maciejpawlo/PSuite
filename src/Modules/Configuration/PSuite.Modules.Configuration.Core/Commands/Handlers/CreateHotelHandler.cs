using Microsoft.Extensions.Logging;
using PSuite.Modules.Configuration.Core.DAL;
using PSuite.Modules.Configuration.Core.Entities;
using PSuite.Modules.Configuration.Core.Events;
using PSuite.Shared.Abstractions.Commands;
using PSuite.Shared.Abstractions.Messaging;

namespace PSuite.Modules.Configuration.Core.Commands.Handlers;

internal sealed class CreateHotelHandler(
    ConfigurationDbContext dbContext,
    IMessageBroker messageBroker,
    ILogger<CreateHotelHandler> logger)
    : ICommandHandler<CreateHotel, Guid>
{
    private static readonly TimeSpan PublishTimeout = TimeSpan.FromSeconds(5);

    public async Task<Guid> HandleAsync(CreateHotel command, CancellationToken cancellationToken = default)
    {
        var hotel = new Hotel(command.Name);
        dbContext.Hotels.Add(hotel);
        await dbContext.SaveChangesAsync(cancellationToken);

        // Already committed: a client disconnect must not lose the event, and a full channel must not hang the request.
        using var publishTimeout = new CancellationTokenSource(PublishTimeout);
        try
        {
            await messageBroker.PublishAsync(new HotelCreated(hotel.Id, hotel.Name), publishTimeout.Token);
        }
        catch (OperationCanceledException) when (publishTimeout.IsCancellationRequested)
        {
            logger.LogWarning("Timed out publishing {EventName} for hotel {HotelId}.", nameof(HotelCreated), hotel.Id);
        }

        return hotel.Id;
    }
}
