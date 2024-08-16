using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Options;
using PSuite.Modules.Configuration.Core.Keycloak.Requests;

namespace PSuite.Modules.Configuration.Core.Keycloak;

internal class KeycloakService(HttpClient httpClient, IOptions<KeycloakOptions> keycloakOptions) : IKeycloakService
{
    private readonly HttpClient httpClient = httpClient;
    private readonly KeycloakOptions keycloakOptions = keycloakOptions.Value;

    public async Task CreateUser(KeycloakUser keycloakUser)
    {
        var result = await httpClient.PostAsJsonAsync($"admin/realms/{keycloakOptions.Realm}/users", keycloakUser);
        result.EnsureSuccessStatusCode();
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
