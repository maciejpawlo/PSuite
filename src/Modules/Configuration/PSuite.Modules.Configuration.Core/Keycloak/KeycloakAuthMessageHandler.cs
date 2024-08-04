using System.Net.Http.Json;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using PSuite.Modules.Configuration.Core.Keycloak.Responses;

namespace PSuite.Modules.Configuration.Core.Keycloak;

internal class KeycloakAuthMessageHandler(IOptions<KeycloakOptions> keycloakOptions, 
    HttpClient httpClient,
    IMemoryCache memoryCache) : DelegatingHandler
{
    private readonly KeycloakOptions keycloakOptions = keycloakOptions.Value;
    private readonly HttpClient httpClient = httpClient;
    private readonly IMemoryCache memoryCache = memoryCache;
    private const string ACCESS_TOKEN_CACHE_KEY = "keycloak_admin_access_token";

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var accessToken = await GetAccessToken(cancellationToken);
        request.Headers.Add("Bearer", accessToken);
        return await base.SendAsync(request, cancellationToken);
    }

    private async Task<string> GetAccessToken(CancellationToken cancellationToken)
    {
        if (memoryCache.TryGetValue(ACCESS_TOKEN_CACHE_KEY, out KeycloakToken? tokenFromCache))
        {
            return tokenFromCache!.AccessToken;
        }
        var token = await GetAccessTokenFromApi(cancellationToken);
        memoryCache.Set(ACCESS_TOKEN_CACHE_KEY, token, TimeSpan.FromSeconds(token.ExpiresIn));
        return token.AccessToken;
    }

    private async Task<KeycloakToken> GetAccessTokenFromApi(CancellationToken cancellationToken)
    {
        var formData = new Dictionary<string, string>
        {
            { "client_id", "admin_cli"},
            { "client_secret", $"{keycloakOptions.ClientSecret}"},
            { "grant_type", "client_credentials" }
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
