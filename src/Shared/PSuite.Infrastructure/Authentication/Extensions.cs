using System.Runtime.CompilerServices;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.JsonWebTokens;

[assembly: InternalsVisibleTo("PSuite.Bootstrapper")]
namespace PSuite.Shared.Infrastructure.Authentication;

internal static class Extensions
{
    internal static IServiceCollection AddAuth(this IServiceCollection services, AuthOptions? authOptions)
    {
        JsonWebTokenHandler.DefaultInboundClaimTypeMap.Clear();

        ArgumentNullException.ThrowIfNull(authOptions);

        services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.Authority = authOptions.Authority;
                options.RequireHttpsMetadata = authOptions.RequireHttpsMetadata;
                options.TokenValidationParameters = new ()
                {            
                    ValidateIssuerSigningKey = authOptions.ValidateIssuerSigningKey,
                    NameClaimType =  authOptions.NameClaimType,
                    RoleClaimType = authOptions.RoleClaimType,
                    ValidAudiences = authOptions.ValidAudiences,
                    ValidIssuers = authOptions.ValidIssuers,
                };
            });
        services.AddAuthorization();
        return services;
    }
}
