using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace PSuite.Shared.Abstractions.Modules;

public interface IModule
{
    public string Name { get; }
    public string BasePath { get; }

    public void Register(IServiceCollection services);
    void Use(IApplicationBuilder app);
    void RegisterEndpoints(IEndpointRouteBuilder endpointRouteBuilder);
}
