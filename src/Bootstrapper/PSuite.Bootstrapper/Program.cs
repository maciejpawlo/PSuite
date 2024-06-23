using System.Reflection;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.JsonWebTokens;
using PSuite.Bootstrapper;
using PSuite.Shared.Abstractions.Modules;
using PSuite.Shared.Infrastructure.Configuration;


var builder = WebApplication.CreateBuilder(args);

JsonWebTokenHandler.DefaultInboundClaimTypeMap.Clear();
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
    options.Authority = "http://localhost:8080/realms/PSuite";
    options.RequireHttpsMetadata = false;
    options.TokenValidationParameters = new ()
    {            
        ValidateIssuerSigningKey = true,
        NameClaimType = "preffered_name",
        RoleClaimType = "role",
        ValidAudiences = ["account"],
        ValidIssuers = ["http://localhost:8080/realms/PSuite"],
    };
    });
builder.Services.AddAuthorization();

builder.AddModulesConfiguration();
IEnumerable<Assembly> assemblies = ModuleLoader.LoadAssemblies(builder.Configuration);
IEnumerable<IModule> modules = ModuleLoader.LoadModules(assemblies);

foreach(var module in modules)
{
    module.Register(builder.Services);
}

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

app.MapControllers();
app.MapGet("/", () => "PSuite API!");
app.MapGet("/test", () => "Routing works!")
    .RequireAuthorization();

app.Run();
