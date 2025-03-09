using System.Text.Json.Serialization;

namespace PSuite.Modules.Configuration.Core.Configuration;

internal class KeycloakOptions
{
    public const string SectionName = "Keycloak";
    public required string Url { get; set; }
    public required string ClientSecret { get; set; }
    public required string Realm { get; set; }
}
