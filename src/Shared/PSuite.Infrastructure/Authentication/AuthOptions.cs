namespace PSuite.Shared.Infrastructure.Authentication;

internal class AuthOptions
{
    public const string SectionName = "Auth";

    public string Authority { get; set; } = string.Empty;
    public bool RequireHttpsMetadata { get; set; }
    public bool ValidateIssuerSigningKey { get; set; }
    public string NameClaimType { get; set; } = string.Empty;
    public string RoleClaimType { get; set; } = string.Empty;
    public string[] ValidAudiences { get; set; } = [];
    public string[] ValidIssuers { get; set; } = [];
}
