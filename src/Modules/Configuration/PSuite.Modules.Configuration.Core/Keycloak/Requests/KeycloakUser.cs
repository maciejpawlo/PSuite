using System.Text.Json.Serialization;

namespace PSuite.Modules.Configuration.Core.Keycloak.Requests;

internal record class KeycloakUser(
    [property: JsonPropertyName("id")] Guid Id,
    [property: JsonPropertyName("firstName")] string FirstName,
    [property: JsonPropertyName("lastName")] string LastName,
    [property: JsonPropertyName("username")] string UserName,
    [property: JsonPropertyName("email")] string Email,
    [property: JsonPropertyName("enabled")] bool Enabled,
    [property: JsonPropertyName("emailVerified")] bool EmailVerified);
