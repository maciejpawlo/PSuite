using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using PSuite.Modules.Configuration.Api.Endpoints;
using PSuite.Modules.Configuration.Core;
using PSuite.Shared.Abstractions.Modules;

namespace PSuite.Modules.Configuration.Api;

public class ConfigurationModule : IModule
{
    public string Name => "Configuration";
    public string BasePath => "configuration-module";

    public void Register(IServiceCollection services)
    {
        services.AddCore();
    }

    public void Use(IApplicationBuilder app)
    {
    }
  
    public void RegisterEndpoints(IEndpointRouteBuilder endpointRouteBuilder)
    {
        endpointRouteBuilder.MapGet(BasePath, () => $"{Name} API");
        endpointRouteBuilder.RegisterHotelApi(BasePath);
        endpointRouteBuilder.RegisterRoomApi(BasePath);
        endpointRouteBuilder.RegisterEmployeeApi(BasePath);
    }
}
