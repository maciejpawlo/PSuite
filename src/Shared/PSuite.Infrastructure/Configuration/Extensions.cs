using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("PSuite.Bootstrapper")]
namespace PSuite.Shared.Infrastructure.Configuration;
internal static class Extensions
{   
    internal static IHostApplicationBuilder AddModulesConfiguration(this IHostApplicationBuilder builder)
    {
        var configurationFiles = Directory.GetFiles(AppDomain.CurrentDomain.BaseDirectory, "module.*.appsettings.json", SearchOption.AllDirectories);
        foreach (var file in configurationFiles)
        {
            builder.Configuration.AddJsonFile(file);
        }
        var env = builder.Environment.EnvironmentName;
        var environmentConfigurationFiles = Directory.GetFiles(AppDomain.CurrentDomain.BaseDirectory, $"module.*.appsettings.{env}.json", SearchOption.AllDirectories);
         
        foreach (var file in environmentConfigurationFiles)
        {
            builder.Configuration.AddJsonFile(file);
        }
        return builder;
    }    
}
