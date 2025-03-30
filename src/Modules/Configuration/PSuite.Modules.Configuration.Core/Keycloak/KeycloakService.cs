using System.Net.Http.Json;
using Microsoft.Extensions.Options;
using Microsoft.Net.Http.Headers;
using PSuite.Modules.Configuration.Core.Configuration;
using PSuite.Modules.Configuration.Core.Keycloak.Requests;

namespace PSuite.Modules.Configuration.Core.Keycloak;

internal class KeycloakService(HttpClient httpClient, IOptions<KeycloakOptions> keycloakOptions) : IKeycloakService
{
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

    public async Task SendExecuteActionsEmail(Guid userId, params string[] actions)
    {
        var result = await httpClient.PutAsJsonAsync($"admin/realms/{keycloakOptions.Realm}/users/{userId}/execute-actions-email", actions);
        result.EnsureSuccessStatusCode();
    }
    
    public async Task AssignRealmRoles(Guid userId, params string[] roles)
    {
        var keycloakRoles = await httpClient.GetFromJsonAsync<List<KeycloakRole>>($"admin/realms/{keycloakOptions.Realm}/roles");
        var roleMappings = keycloakRoles!
            .Where(x => roles.Contains(x.Name))
            .Select(x => new KeycloakRole(x.Id, x.Name));
        var result = await httpClient.PostAsJsonAsync($"admin/realms/{keycloakOptions.Realm}/users/{userId}/role-mappings/realm", roleMappings);
        result.EnsureSuccessStatusCode();
    }
}
