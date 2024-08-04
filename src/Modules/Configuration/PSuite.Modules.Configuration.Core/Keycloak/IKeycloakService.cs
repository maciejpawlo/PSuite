using PSuite.Modules.Configuration.Core.Keycloak.Requests;

namespace PSuite.Modules.Configuration.Core.Keycloak;

internal interface IKeycloakService
{
    Task CreateUser(KeycloakUser keycloakUser);
}
