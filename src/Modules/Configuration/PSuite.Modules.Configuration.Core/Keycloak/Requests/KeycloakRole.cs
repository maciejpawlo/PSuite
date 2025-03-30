using System.Text.Json.Serialization;

namespace PSuite.Modules.Configuration.Core.Keycloak.Requests;

internal record class KeycloakRole(Guid Id, string Name);