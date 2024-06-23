using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using PSuite.Shared.Abstractions.Modules;

namespace PSuite.Modules.Configuration.Api;

public class ConfigurationModule : IModule
{
    public string Name => "Configuration";
    public string BasePath => "configuration-module";

    public void Register(IServiceCollection services)
    {
    }

    public void Use(IApplicationBuilder app)
    {
    }
  
    public void RegisterEndpoints(IEndpointRouteBuilder endpointRouteBuilder)
    {
    }
}
