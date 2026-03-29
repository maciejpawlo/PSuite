using PSuite.APIGateway;
using PSuite.APIGateway.Configuration;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi();

builder.Services.AddHttpContextAccessor();

builder.Services.AddReverseProxy(builder.Configuration);
builder.Services.AddOAuthProxy();
builder.Services.AddAuthorizationPolicies();
builder.Services.AddCache(builder.Configuration);

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.UseAuthentication();
app.UseAuthorization();

app.MapReverseProxy();
app.RegisterAccountApi();

app.Run();
