using PSuite.Modules.Configuration.Core.Keycloak.Requests;

namespace PSuite.Modules.Configuration.Core.Keycloak;

internal interface IKeycloakService
{
    Task CreateUser(KeycloakUser keycloakUser);
    Task UpdateUser(KeycloakUser keycloakUser);
    Task DeleteUser(Guid userId);
}
