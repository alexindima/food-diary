using System.Reflection;
using System.Security.Claims;
using FoodDiary.Presentation.Api.Policies;
using FoodDiary.Web.Api.Options;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.OutputCaching;

namespace FoodDiary.Web.Api.Tests.Extensions;

[ExcludeFromCodeCoverage]
public sealed class OutputCacheOptionsSetupTests {
    [Fact]
    public async Task Configure_UserScopedPolicy_UsesAuthenticatedUserIdAndConfiguredExpiration() {
        var userId = Guid.NewGuid();
        var options = new OutputCacheOptions();
        new OutputCacheOptionsSetup(Microsoft.Extensions.Options.Options.Create(new ApiOutputCacheOptions {
            UserScoped = new ApiOutputCacheOptions.UserScopedCacheOptions {
                ExpirationSeconds = 17,
            },
        })).Configure(options);
        IOutputCachePolicy policy = FindPolicy(options, PresentationPolicyNames.UserScopedCachePolicyName);
        var context = new OutputCacheContext {
            HttpContext = CreateAuthenticatedContext(userId, "Bearer token"),
        };

        await policy.CacheRequestAsync(context, CancellationToken.None);

        Assert.Multiple(
            () => Assert.True(context.EnableOutputCaching),
            () => Assert.Equal(TimeSpan.FromSeconds(17), context.ResponseExpirationTimeSpan),
            () => Assert.Equal("*", context.CacheVaryByRules.QueryKeys.ToString()),
            () => Assert.Equal(userId.ToString("D"), context.CacheVaryByRules.VaryByValues["user-id"]),
            () => Assert.Contains("user-scoped", context.Tags));
    }

    [Fact]
    public async Task Configure_UserScopedPolicy_DoesNotCacheWithoutUserIdentity() {
        var options = new OutputCacheOptions();
        new OutputCacheOptionsSetup(Microsoft.Extensions.Options.Options.Create(new ApiOutputCacheOptions())).Configure(options);
        IOutputCachePolicy policy = FindPolicy(options, PresentationPolicyNames.UserScopedCachePolicyName);
        var context = new OutputCacheContext {
            HttpContext = new DefaultHttpContext(),
        };

        await policy.CacheRequestAsync(context, CancellationToken.None);

        Assert.False(context.EnableOutputCaching);
        Assert.Empty(context.CacheVaryByRules.VaryByValues);
    }

    [Fact]
    public void GetUserCacheVaryByValue_UsesStableUserIdInsteadOfAuthorizationHeader() {
        var userId = Guid.NewGuid();
        HttpContext firstContext = CreateAuthenticatedContext(userId, "Bearer first-token");
        HttpContext refreshedContext = CreateAuthenticatedContext(userId, "Bearer refreshed-token");
        HttpContext otherUserContext = CreateAuthenticatedContext(Guid.NewGuid(), "Bearer first-token");

        KeyValuePair<string, string> first = InvokeGetUserCacheVaryByValue(firstContext);
        KeyValuePair<string, string> refreshed = InvokeGetUserCacheVaryByValue(refreshedContext);
        KeyValuePair<string, string> otherUser = InvokeGetUserCacheVaryByValue(otherUserContext);

        Assert.Multiple(
            () => Assert.Equal("user-id", first.Key),
            () => Assert.Equal(userId.ToString("D"), first.Value),
            () => Assert.Equal(first, refreshed),
            () => Assert.NotEqual(first, otherUser));
    }

    [Fact]
    public void HasUserCacheIdentity_RequiresValidUserIdClaim() {
        var anonymous = new OutputCacheContext {
            HttpContext = new DefaultHttpContext(),
        };
        var authenticated = new OutputCacheContext {
            HttpContext = CreateAuthenticatedContext(Guid.NewGuid(), "Bearer token"),
        };

        Assert.Multiple(
            () => Assert.False(InvokeHasUserCacheIdentity(anonymous)),
            () => Assert.True(InvokeHasUserCacheIdentity(authenticated)));
    }

    private static HttpContext CreateAuthenticatedContext(Guid userId, string authorization) {
        var context = new DefaultHttpContext {
            User = new ClaimsPrincipal(new ClaimsIdentity(
                [new Claim(ClaimTypes.NameIdentifier, userId.ToString("D"))],
                "test")),
        };
        context.Request.Headers.Authorization = authorization;
        return context;
    }

    private static KeyValuePair<string, string> InvokeGetUserCacheVaryByValue(HttpContext context) {
        MethodInfo? method = typeof(OutputCacheOptionsSetup).GetMethod(
            "GetUserCacheVaryByValue",
            BindingFlags.NonPublic | BindingFlags.Static);

        Assert.NotNull(method);
        return Assert.IsType<KeyValuePair<string, string>>(method.Invoke(null, [context]));
    }

    private static bool InvokeHasUserCacheIdentity(OutputCacheContext context) {
        MethodInfo? method = typeof(OutputCacheOptionsSetup).GetMethod(
            "HasUserCacheIdentity",
            BindingFlags.NonPublic | BindingFlags.Static);

        Assert.NotNull(method);
        return Assert.IsType<bool>(method.Invoke(null, [context]));
    }

    private static IOutputCachePolicy FindPolicy(OutputCacheOptions options, string policyName) {
        PropertyInfo? property = typeof(OutputCacheOptions).GetProperty(
            "NamedPolicies",
            BindingFlags.NonPublic | BindingFlags.Instance);

        Assert.NotNull(property);
        Dictionary<string, IOutputCachePolicy> policies =
            Assert.IsType<Dictionary<string, IOutputCachePolicy>>(property.GetValue(options));
        return policies[policyName];
    }
}
