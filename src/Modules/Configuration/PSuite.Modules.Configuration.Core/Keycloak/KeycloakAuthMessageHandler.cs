using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Microsoft.Net.Http.Headers;
using PSuite.Modules.Configuration.Core.Keycloak.Responses;

namespace PSuite.Modules.Configuration.Core.Keycloak;

internal class KeycloakAuthMessageHandler(IOptions<KeycloakOptions> keycloakOptions, 
    HttpClient httpClient,
    IDistributedCache distributedCache) : DelegatingHandler
{
    private readonly KeycloakOptions keycloakOptions = keycloakOptions.Value;
    private readonly HttpClient httpClient = httpClient;
    private readonly IDistributedCache distributedCache = distributedCache;

    private const string ACCESS_TOKEN_CACHE_KEY = "_keycloak_admin_access_token";

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var accessToken = await GetAccessToken(cancellationToken);
        request.Headers.Add("Authorization", $"Bearer {accessToken}");
        return await base.SendAsync(request, cancellationToken);
    }

    private async Task<string> GetAccessToken(CancellationToken cancellationToken)
    {
        var tokenFromCache = await distributedCache.GetStringAsync(ACCESS_TOKEN_CACHE_KEY, cancellationToken);
        if (tokenFromCache is not null)
        {
            return JsonSerializer.Deserialize<KeycloakToken>(tokenFromCache)!.AccessToken;
        }
        var token = await GetAccessTokenFromApi(cancellationToken);
        var cacheEntryOptions = new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(token.ExpiresIn)
        };
        await distributedCache.SetStringAsync(ACCESS_TOKEN_CACHE_KEY, JsonSerializer.Serialize(token), cacheEntryOptions, cancellationToken);
        return token.AccessToken;
    }

    private async Task<KeycloakToken> GetAccessTokenFromApi(CancellationToken cancellationToken)
    {
        var formData = new Dictionary<string, string>
        {
            { "grant_type", "client_credentials" },
            { "client_id", "admin-cli"},
            { "client_secret", $"{keycloakOptions.ClientSecret}"}
        };
        var content = new FormUrlEncodedContent(formData);
        httpClient.BaseAddress = new Uri(keycloakOptions.Url);
        var result = await httpClient.PostAsync($"realms/{keycloakOptions.Realm}/protocol/openid-connect/token", content, cancellationToken);
        result.EnsureSuccessStatusCode();
        var token = await result.Content.ReadFromJsonAsync<KeycloakToken>(cancellationToken);
        ArgumentNullException.ThrowIfNull(token);
        return token;
    }
}
