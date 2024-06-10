using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.Extensions.Caching.Distributed;

namespace PSuite.APIGateway;

public class RedisSessionStore : ITicketStore
{    
    private const string KeyPrefix = "_oauth2_proxy-";
    
    private readonly IServiceCollection _services;
    private readonly DistributedCacheEntryOptions _cacheEntryOptions;

    public RedisSessionStore(IServiceCollection services)
    {
        _services = services;
        _cacheEntryOptions = new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(60)
        };
    }

    public async Task RemoveAsync(string key)
    {
        using var scope = _services.BuildServiceProvider().CreateScope();
        
        var distributedCache = scope.ServiceProvider.GetService<IDistributedCache>()!;
        
        var ticket = await distributedCache.GetAsync(key);
        if (ticket is not null)
        {
            await distributedCache.RemoveAsync(key);
        }
    }

    public async Task RenewAsync(string key, AuthenticationTicket ticket)
    {
        using var scope = _services.BuildServiceProvider().CreateScope();
        
        var distributedCache = scope.ServiceProvider.GetService<IDistributedCache>()!;
        
        await distributedCache.SetAsync(key, TicketSerializer.Default.Serialize(ticket), _cacheEntryOptions);
    }

    public async Task<AuthenticationTicket?> RetrieveAsync(string key)
    {
        using var scope = _services.BuildServiceProvider().CreateScope();
        var distributedCache = scope.ServiceProvider.GetService<IDistributedCache>()!;
        
        var cachedMember = await distributedCache.GetAsync(key);
        if (cachedMember is null)
        {
            return null;
        }

        var ticket = TicketSerializer.Default.Deserialize(cachedMember);
        return ticket;
    }

    public async Task<string> StoreAsync(AuthenticationTicket ticket)
    {
        using var scope = _services.BuildServiceProvider().CreateScope();
        
        var distributedCache = scope.ServiceProvider.GetService<IDistributedCache>()!;
        
        var key = KeyPrefix + Guid.NewGuid();

        var serializedTicket = TicketSerializer.Default.Serialize(ticket);
        await distributedCache.SetAsync(key, serializedTicket, _cacheEntryOptions);
        
        return key;
    }
}
