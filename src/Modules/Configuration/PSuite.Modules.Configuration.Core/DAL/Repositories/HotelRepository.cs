using Microsoft.EntityFrameworkCore;
using PSuite.Modules.Configuration.Core.Entities;
using PSuite.Modules.Configuration.Core.Repositories;

namespace PSuite.Modules.Configuration.Core.DAL.Repositories;

internal class HotelRepository(ConfigurationDbContext dbContext) : IHotelRepository
{
    private readonly ConfigurationDbContext dbContext = dbContext;

    public async Task CreateAsync(Hotel hotel)
    {
        await dbContext.AddAsync(hotel);
        await dbContext.SaveChangesAsync();
    }

    public async Task DeleteAsync(Hotel hotel)
    {
        dbContext.Hotels.Remove(hotel);
        await dbContext.SaveChangesAsync();
    }

    public async Task<IEnumerable<Hotel>> GetAllAsync()
        => await dbContext.Hotels.ToListAsync();

    public Task<Hotel?> GetByIdAsync(Guid id)
        => dbContext.Hotels
            .AsNoTracking()
            .Include(x => x.Employees)
            .Include(x => x.Rooms)
            .FirstOrDefaultAsync(x => x.Id == id);

    public async Task UpdateAsync(Hotel hotel)
    {
        dbContext.Hotels.Update(hotel);
        await dbContext.SaveChangesAsync();
    }
}
