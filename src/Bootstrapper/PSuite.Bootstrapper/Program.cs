using System.Reflection;
using PSuite.Bootstrapper;
using PSuite.Shared.Abstractions.Modules;
using PSuite.Shared.Infrastructure.Configuration;
using PSuite.Shared.Infrastructure;
using PSuite.Shared.Infrastructure.Modules;


var builder = WebApplication.CreateBuilder(args);

builder.AddModulesConfiguration();

IEnumerable<Assembly> assemblies = ModuleLoader.LoadAssemblies(builder.Configuration);
IEnumerable<IModule> modules = ModuleLoader.LoadModules(assemblies);

foreach(var module in modules)
{
    module.Register(builder.Services);
}
builder.Services.AddInfrastructure(modules, assemblies);

var app = builder.Build();

foreach(var module in modules)
{
    module.Use(app);
    module.RegisterEndpoints(app);
}

app.UseHttpsRedirection();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.UseInfrastructure();

app.MapGet("/", () => "PSuite API!");
app.MapModuleInfo();
app.MapGet("/test", () => "Routing works!")
    .RequireAuthorization();

app.Run();
