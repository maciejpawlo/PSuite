using System.Net.Http.Json;
using Microsoft.Extensions.Options;
using PSuite.Modules.Configuration.Core.Keycloak.Requests;

namespace PSuite.Modules.Configuration.Core.Keycloak;

internal class KeycloakService(HttpClient httpClient, IOptions<KeycloakOptions> keycloakOptions) : IKeycloakService
{
    private readonly HttpClient httpClient = httpClient;
    private readonly KeycloakOptions keycloakOptions = keycloakOptions.Value;

    public async Task CreateUser(KeycloakUser keycloakUser)
    {
        var result = await httpClient.PostAsJsonAsync($"admin/{keycloakOptions.Realm}/users", keycloakUser);
        result.EnsureSuccessStatusCode();
    }
}
