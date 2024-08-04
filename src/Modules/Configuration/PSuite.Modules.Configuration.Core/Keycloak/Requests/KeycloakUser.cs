namespace PSuite.Modules.Configuration.Core.Keycloak.Requests;

internal record class KeycloakUser(
    Guid Id,
    string FirstName,
    string LastName,
    string Email,
    bool Enabled,
    bool EmailVerified);
