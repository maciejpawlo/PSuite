using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;

namespace PSuite.APIGateway;

public static class AccountApi
{
    public static void RegisterAccountApi(this IEndpointRouteBuilder app)
    {
        var accountEndpoints = app.MapGroup("account")
            .WithTags("Account")
            .WithOpenApi()
            .WithMetadata();

        accountEndpoints.MapGet("/public", () => "Welcome to API Gateway")
            .WithName("Public");

        accountEndpoints.MapGet("/logout", (Delegate) HandleLogout)
            .WithName("Logout")
            .RequireAuthorization();
    }
    private static async Task HandleLogout(HttpContext context)
    {
        var prop = new AuthenticationProperties
        {
            RedirectUri = "/account/public"
        };

        if(context.Request.Cookies.Count > 0)
        {
            var siteCookies = context.Request.Cookies.Where(c => c.Key.Contains(".AspNetCore.") || c.Key.Contains("Microsoft.Authentication"));
            foreach (var cookie in siteCookies)
            {
                context.Response.Cookies.Delete(cookie.Key);
            }
        }
        await context.SignOutAsync(OpenIdConnectDefaults.AuthenticationScheme, prop);
        await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    }
}
