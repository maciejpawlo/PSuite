namespace PSuite.Shared.Infrastructure.Database;

internal class PostgresOptions
{
    public const string SectionName = "postgres";
    public string ConnectionString { get; set; } = string.Empty;
}
