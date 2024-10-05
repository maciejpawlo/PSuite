namespace PSuite.Modules.Configuration.Core;

internal class KeycloakOptions
{
    public const string SectionName = "configuration:keycloak";
    public required string Url { get; set; }
    public required string ClientSecret { get; set; }
    public required string Realm { get; set; }
}
