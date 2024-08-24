using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Options;
using Microsoft.Net.Http.Headers;
using PSuite.Modules.Configuration.Core.Keycloak.Requests;

namespace PSuite.Modules.Configuration.Core.Keycloak;

internal class KeycloakService(HttpClient httpClient, IOptions<KeycloakOptions> keycloakOptions) : IKeycloakService
{
    private readonly HttpClient httpClient = httpClient;
    private readonly KeycloakOptions keycloakOptions = keycloakOptions.Value;

    public async Task<Guid> CreateUser(KeycloakUser keycloakUser)
    {
        var result = await httpClient.PostAsJsonAsync($"admin/realms/{keycloakOptions.Realm}/users", keycloakUser);
        result.EnsureSuccessStatusCode();
        var locationUri = result.Headers.GetValues(HeaderNames.Location).FirstOrDefault();
        var lastSlashIndex = locationUri!.LastIndexOf('/');
        return Guid.Parse(locationUri.AsSpan(lastSlashIndex + 1));
    }

    public async Task DeleteUser(Guid userId)
    {
        var result = await httpClient.DeleteAsync($"admin/realms/{keycloakOptions.Realm}/users/{userId}");
        result.EnsureSuccessStatusCode();
    }

    public async Task UpdateUser(KeycloakUser keycloakUser)
    {
        var result = await httpClient.PutAsJsonAsync($"admin/realms/{keycloakOptions.Realm}/users/{keycloakUser.Id}", keycloakUser);
        result.EnsureSuccessStatusCode();
    }
}
