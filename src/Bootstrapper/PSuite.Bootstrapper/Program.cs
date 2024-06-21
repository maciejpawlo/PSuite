using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.JsonWebTokens;
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
//TODO: load modules assemblies
var app = builder.Build();

app.UseHttpsRedirection();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/", () => "Hello World!");
app.MapGet("/test", () => "Routing works!")
    .RequireAuthorization();

app.Run();
