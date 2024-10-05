namespace PSuite.Shared.Infrastructure;

internal class RedisOptions
{
    public const string SectionName = "Redis";
    public string ConnectionString { get; set; } = string.Empty;
}
