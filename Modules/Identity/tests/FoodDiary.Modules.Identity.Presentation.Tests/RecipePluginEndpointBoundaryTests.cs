using System.Security.Claims;
using FoodDiary.Modules.Identity.Application.Abstractions.Authentication.Common;
using FoodDiary.Modules.Identity.Contracts.Authentication.Common;
using FoodDiary.Modules.Identity.Presentation.Security;
using FoodDiary.Presentation.Api.Policies;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Primitives;
using OpenIddict.Abstractions;
using OpenIddict.Server;
using OpenIddict.Server.AspNetCore;

namespace FoodDiary.Modules.Identity.Presentation.Tests;

[ExcludeFromCodeCoverage]
public sealed class RecipePluginEndpointBoundaryTests {
    [Theory]
    [InlineData(200, null)]
    [InlineData(302, "/relative-callback")]
    [InlineData(302, "https://other.example.test/callback")]
    public async Task NavigatedConsentPreservesUnexpectedAuthorizationResponsesWithoutRenderingAContinuation(int status, string? location) {
        const string callback = "https://callback.example.test/";
        var identity = new ClaimsIdentity(BrowserSessionCookieService.Scheme);
        identity.AddClaim(new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString("D")));
        identity.AddClaim(new Claim(JwtSecurityClaimNames.RefreshSessionId, Guid.NewGuid().ToString("D")));
        IAuthenticationService authentication = Substitute.For<IAuthenticationService>();
        authentication.AuthenticateAsync(Arg.Any<HttpContext>(), BrowserSessionCookieService.Scheme)
            .Returns(AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(identity), BrowserSessionCookieService.Scheme)));
        authentication.ForbidAsync(Arg.Any<HttpContext>(), Arg.Any<string>(), Arg.Any<AuthenticationProperties>())
            .Returns(call => {
                HttpResponse response = call.ArgAt<HttpContext>(0).Response;
                response.StatusCode = status;
                response.Headers.Location = location;
                return Task.CompletedTask;
            });
        WebApplicationBuilder builder = WebApplication.CreateBuilder();
        builder.Services.AddSingleton(authentication);
        builder.Services.AddSingleton(Substitute.For<IAntiforgery>());
        builder.Services.AddSingleton(Substitute.For<IDelegatedAccessTokenValidator>());
        await using WebApplication app = builder.Build();
        app.MapRecipePluginOAuth(new Uri("https://mcp.example.test/"), new Uri("https://app.example.test/"));
        RouteEndpoint endpoint = ((IEndpointRouteBuilder)app).DataSources.SelectMany(source => source.Endpoints)
            .OfType<RouteEndpoint>().Single(candidate => string.Equals(candidate.RoutePattern.RawText, RecipePluginAccess.AuthorizationPath, StringComparison.Ordinal));
        var request = new OpenIddictRequest { RedirectUri = callback, Scope = RecipePluginAccess.RecipeScope };
        request.SetParameter(OpenIddictConstants.Parameters.Resource, "https://mcp.example.test" + RecipePluginAccess.ResourcePath);
        var context = new DefaultHttpContext { RequestServices = app.Services };
        context.Request.Method = HttpMethods.Post;
        context.Request.Headers["Sec-Fetch-Mode"] = "navigate";
        context.Request.Form = new FormCollection(new Dictionary<string, StringValues>(StringComparer.Ordinal) {
            ["fooddiary_decision"] = "deny",
        });
        context.Features.Set(new OpenIddictServerAspNetCoreFeature { Transaction = new OpenIddictServerTransaction { Request = request } });
        await using var body = new MemoryStream();
        context.Response.Body = body;

        await endpoint.RequestDelegate!(context);

        Assert.Equal(status, context.Response.StatusCode);
        Assert.Equal(location ?? string.Empty, context.Response.Headers.Location.ToString());
        Assert.Equal(0, body.Length);
        await authentication.Received(1).ForbidAsync(context, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme, Arg.Any<AuthenticationProperties>());
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task MappedOAuthEndpointsRequireTheMiddlewareToProvideAProtocolRequest(bool authorize) {
        WebApplicationBuilder builder = WebApplication.CreateBuilder();
        builder.Services.AddSingleton(Substitute.For<IAntiforgery>());
        builder.Services.AddSingleton(Substitute.For<IDelegatedAccessTokenValidator>());
        await using WebApplication app = builder.Build();
        app.MapRecipePluginOAuth(new Uri("https://mcp.example.test/"), new Uri("https://app.example.test/"));
        string path = authorize ? RecipePluginAccess.AuthorizationPath : RecipePluginAccess.TokenPath;
        RouteEndpoint endpoint = ((IEndpointRouteBuilder)app).DataSources.SelectMany(source => source.Endpoints)
            .OfType<RouteEndpoint>().Single(candidate => string.Equals(candidate.RoutePattern.RawText, path, StringComparison.Ordinal));
        var context = new DefaultHttpContext { RequestServices = app.Services };

        await endpoint.RequestDelegate!(context);

        Assert.Equal(StatusCodes.Status404NotFound, context.Response.StatusCode);
    }

    [Theory]
    [InlineData(false, "invalid_target")]
    [InlineData(true, "invalid_grant")]
    public async Task TokenEndpointRejectsAnUnboundResourceOrMissingAuthenticatedGrant(bool validResource, string expectedError) {
        var resource = new Uri("https://mcp.example.test" + RecipePluginAccess.ResourcePath);
        IAuthenticationService authentication = Substitute.For<IAuthenticationService>();
        authentication.AuthenticateAsync(Arg.Any<HttpContext>(), Arg.Any<string>()).Returns(AuthenticateResult.NoResult());
        IDelegatedAccessTokenValidator validator = Substitute.For<IDelegatedAccessTokenValidator>();
        WebApplicationBuilder builder = WebApplication.CreateBuilder();
        builder.Services.AddSingleton(authentication);
        builder.Services.AddSingleton(validator);
        builder.Services.AddSingleton(Substitute.For<IAntiforgery>());
        await using WebApplication app = builder.Build();
        app.MapRecipePluginOAuth(new Uri("https://mcp.example.test/"), new Uri("https://app.example.test/"));
        RouteEndpoint endpoint = ((IEndpointRouteBuilder)app).DataSources.SelectMany(source => source.Endpoints)
            .OfType<RouteEndpoint>().Single(candidate => string.Equals(candidate.RoutePattern.RawText, RecipePluginAccess.TokenPath, StringComparison.Ordinal));
        var request = new OpenIddictRequest();
        request.SetParameter(OpenIddictConstants.Parameters.Resource, validResource ? resource.AbsoluteUri : "https://other.example.test/mcp");
        var context = new DefaultHttpContext { RequestServices = app.Services };
        context.Features.Set(new OpenIddictServerAspNetCoreFeature { Transaction = new OpenIddictServerTransaction { Request = request } });

        await endpoint.RequestDelegate!(context);

        await authentication.Received(1).ForbidAsync(context, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme,
            Arg.Is<AuthenticationProperties>(properties => string.Equals(properties.Items[OpenIddictServerAspNetCoreConstants.Properties.Error], expectedError, StringComparison.Ordinal)));
        Assert.Empty(validator.ReceivedCalls());
    }
}
