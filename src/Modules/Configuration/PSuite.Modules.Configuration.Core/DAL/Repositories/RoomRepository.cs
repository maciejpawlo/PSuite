using Microsoft.EntityFrameworkCore;
using PSuite.Modules.Configuration.Core.Entities;
using PSuite.Modules.Configuration.Core.Repositories;

namespace PSuite.Modules.Configuration.Core.DAL.Repositories;

internal class RoomRepository(ConfigurationDbContext dbContext) : IRoomRepository
{
    private readonly ConfigurationDbContext dbContext = dbContext;

    public async Task CreateAsync(Room room)
    {
        await dbContext.Rooms.AddAsync(room);
        await dbContext.SaveChangesAsync();
    }

    public async Task DeleteAsync(Room room)
    {
        dbContext.Rooms.Remove(room);
        await dbContext.SaveChangesAsync();
    }

    public async Task<IEnumerable<Room>> GetAllAsync()
        => await dbContext.Rooms
            .AsNoTracking()
            .Include(x => x.Hotel)
            .ToListAsync();

    public Task<Room?> GetByIdAsync(Guid id)
        => dbContext.Rooms
            .AsNoTracking()
            .Include(x => x.Hotel)
            .FirstOrDefaultAsync(x => x.Id == id);

    public async Task UpdateAsync(Room room)
    {
        dbContext.Rooms.Update(room);
        await dbContext.SaveChangesAsync();
    }
}
