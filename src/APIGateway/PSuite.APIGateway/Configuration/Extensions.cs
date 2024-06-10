using System.Net.Http.Headers;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using Yarp.ReverseProxy.Transforms;

namespace PSuite.APIGateway.Configuration;

public static class Extensions
{
    private static T GetOptions<T>(this IServiceCollection services, string sectionName) where T : new()
    {
        using var serviceProvider = services.BuildServiceProvider();
        var configuration = serviceProvider.GetRequiredService<IConfiguration>();
        var section = configuration.GetSection(sectionName);
        var options = new T();
        section.Bind(options);
        return options;
    }

    public static void AddCache(this IServiceCollection services, IConfiguration configuration)
    {
        var connection = configuration.GetConnectionString("Redis")!;

        services.AddStackExchangeRedisCache(option =>
        {
            option.Configuration = connection;
        });
    }

    public static void AddOAuthProxy(this IServiceCollection services)
    {
        JsonWebTokenHandler.DefaultInboundClaimTypeMap.Clear();

        var proxyOptions = services.GetOptions<OAuthProxyOptions>(OAuthProxyOptions.SectionName);

        services.AddAuthentication(options => 
        {
            options.DefaultAuthenticateScheme = CookieAuthenticationDefaults.AuthenticationScheme;
            options.DefaultSignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
        })
        .AddCookie(options => 
        {
            options.Cookie.Name = "keycloak.cookie";
            options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
            options.SessionStore = new RedisSessionStore(services);
        })
        .AddOpenIdConnect(options => 
        {
            options.SignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
            options.Authority = proxyOptions.Authority;
            options.RequireHttpsMetadata = false;

            options.ClientId = proxyOptions.ClientId;
            options.ClientSecret = proxyOptions.ClientSecret;
            options.ResponseType = OpenIdConnectResponseType.Code;
            //SameSite is needed for Chrome/Firefox, as they will give http error 500 back, if not set to unspecified.
            options.NonceCookie.SameSite = SameSiteMode.Unspecified;
            options.CorrelationCookie.SameSite = SameSiteMode.Unspecified;

            // options.Scope.Clear();
            options.GetClaimsFromUserInfoEndpoint = true;
            options.Scope.Add("openid");
            options.Scope.Add("profile");

            // options.MapInboundClaims = false; // Don't rename claim types
            // options.CallbackPath = "/signin-oidc"; // Set the callback path
            // options.SignedOutCallbackPath = ""; // Set the sign-out callback path
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                ValidateTokenReplay = true,
                NameClaimType = "preferred_username",
                RoleClaimType = "roles"
            };

            options.SaveTokens = true;
        });

        services.AddSingleton<CookieOidcRefresher>();

        services.AddOptions<CookieAuthenticationOptions>(CookieAuthenticationDefaults.AuthenticationScheme)
            .Configure<CookieOidcRefresher>((cookieOptions, refresher) =>
            {
                cookieOptions.Events.OnValidatePrincipal = context =>
                    refresher.ValidateOrRefreshCookieAsync(context, OpenIdConnectDefaults.AuthenticationScheme);
            });
    }

    public static void AddAuthorizationPolicies(this IServiceCollection services)
    {
        services.AddAuthorizationBuilder()
            .AddPolicy("authenticatedUser", policy => { policy.RequireAuthenticatedUser(); });
    }

    public static void AddReverseProxy(this IServiceCollection services, ConfigurationManager configuration)
    {
        services.AddReverseProxy()
            .LoadFromConfig(configuration.GetSection("ReverseProxy"))
            .AddTransforms(builderContext =>
            {
                builderContext.AddRequestTransform(async transformContext =>
                {
                    var accessToken = await transformContext.HttpContext.GetTokenAsync("access_token");
                    if(accessToken is not null)
                    {
                        transformContext.ProxyRequest.Headers.Remove("Cookie");
                        transformContext.ProxyRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
                    }
                });
            });
            
    }
}
