using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.JsonWebTokens;

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

var app = builder.Build();

app.UseHttpsRedirection();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/", () => "Hello World!");
app.MapGet("/test", () => "Routing works!")
    .RequireAuthorization();

app.Run();
