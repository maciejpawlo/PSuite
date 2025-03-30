using PSuite.Modules.Configuration.Core.Keycloak.Requests;

namespace PSuite.Modules.Configuration.Core.Keycloak;

internal interface IKeycloakService
{
    Task<Guid> CreateUser(KeycloakUser keycloakUser);
    Task UpdateUser(KeycloakUser keycloakUser);
    Task DeleteUser(Guid userId);
    Task SendExecuteActionsEmail(Guid userId, params string[] actions);
    Task AssignRealmRoles(Guid userId, params string[] roles);
}
