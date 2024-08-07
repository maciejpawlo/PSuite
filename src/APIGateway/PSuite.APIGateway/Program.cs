using PSuite.APIGateway;
using PSuite.APIGateway.Configuration;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddHttpContextAccessor();

builder.Services.AddReverseProxy(builder.Configuration);
builder.Services.AddOAuthProxy();
builder.Services.AddAuthorizationPolicies();
builder.Services.AddCache(builder.Configuration);

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthentication();
app.UseAuthorization();

app.MapReverseProxy();
app.RegisterAccountApi();

app.Run();
