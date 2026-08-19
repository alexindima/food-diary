using System.Reflection;
using System.Collections;
using FoodDiary.Presentation.Api.Policies;
using FoodDiary.Web.Api.Options;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;

namespace FoodDiary.Web.Api.Tests.Extensions;

[ExcludeFromCodeCoverage]
public sealed class RateLimiterOptionsSetupTests {
    [Fact]
    public void GetPartitionKey_IgnoresSpoofedForwardedForHeader_AndUsesRemoteIp() {
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Headers["X-Forwarded-For"] = "203.0.113.10";
        httpContext.Connection.RemoteIpAddress = System.Net.IPAddress.Parse("198.51.100.25");

        string partitionKey = InvokeGetPartitionKey(httpContext);

        Assert.Equal("ip:198.51.100.25", partitionKey);
    }

    [Fact]
    public void GetPartitionKey_NormalizesIpv6MappedIpv4Address() {
        var httpContext = new DefaultHttpContext();
        httpContext.Connection.RemoteIpAddress = System.Net.IPAddress.Parse("::ffff:198.51.100.25");

        string partitionKey = InvokeGetPartitionKey(httpContext);

        Assert.Equal("ip:198.51.100.25", partitionKey);
    }

    [Fact]
    public void GetPartitionKey_WhenRemoteIpIsMissing_UsesUnknownIp() {
        var httpContext = new DefaultHttpContext();

        string partitionKey = InvokeGetPartitionKey(httpContext);

        Assert.Equal("ip:unknown", partitionKey);
    }

    [Fact]
    public void GetPartitionKey_WhenAuthenticatedUserHasId_UsesUserId() {
        var userId = Guid.NewGuid();
        var httpContext = new DefaultHttpContext {
            User = new System.Security.Claims.ClaimsPrincipal(
                new System.Security.Claims.ClaimsIdentity(
                    [new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.NameIdentifier, userId.ToString())],
                    "test")),
        };

        string partitionKey = InvokeGetPartitionKey(httpContext);

        Assert.Equal($"user:{userId:D}", partitionKey);
    }

    [Fact]
    public void Configure_WebhookPolicyFactory_CreatesPartition() {
        var options = new RateLimiterOptions();
        new RateLimiterOptionsSetup(Microsoft.Extensions.Options.Options.Create(new ApiRateLimitingOptions())).Configure(options);
        object policy = FindPolicy(options, PresentationPolicyNames.WebhookRateLimitPolicyName);
        Delegate factory = Assert.Single(GetDelegates(policy));
        var httpContext = new DefaultHttpContext();
        httpContext.Connection.RemoteIpAddress = System.Net.IPAddress.Parse("198.51.100.25");

        object? partition = factory.DynamicInvoke(httpContext);

        Assert.NotNull(partition);
    }

    [Fact]
    public void Configure_WebhookPolicyFactory_UsesGlobalPartitionForYooKassa() {
        var options = new RateLimiterOptions();
        new RateLimiterOptionsSetup(Microsoft.Extensions.Options.Options.Create(new ApiRateLimitingOptions())).Configure(options);
        object policy = FindPolicy(options, PresentationPolicyNames.WebhookRateLimitPolicyName);
        Delegate factory = Assert.Single(GetDelegates(policy));
        DefaultHttpContext firstContext = CreateWebhookContext("YooKassa", "198.51.100.25");
        DefaultHttpContext secondContext = CreateWebhookContext("yookassa", "203.0.113.10");
        DefaultHttpContext stripeContext = CreateWebhookContext("stripe", "198.51.100.25");

        string firstPartitionKey = GetPartitionKey(factory.DynamicInvoke(firstContext));
        string secondPartitionKey = GetPartitionKey(factory.DynamicInvoke(secondContext));
        string stripePartitionKey = GetPartitionKey(factory.DynamicInvoke(stripeContext));

        Assert.Multiple(
            () => Assert.Equal("webhook:provider:yookassa", firstPartitionKey),
            () => Assert.Equal(firstPartitionKey, secondPartitionKey),
            () => Assert.False(string.Equals(firstPartitionKey, stripePartitionKey, StringComparison.Ordinal)));
    }

    [Theory]
    [InlineData(PresentationPolicyNames.ClientTelemetryRateLimitPolicyName)]
    [InlineData(PresentationPolicyNames.MarketingAttributionRateLimitPolicyName)]
    [InlineData(PresentationPolicyNames.TestDeliveryRateLimitPolicyName)]
    [InlineData(PresentationPolicyNames.WearableRateLimitPolicyName)]
    [InlineData(PresentationPolicyNames.FoodDataRateLimitPolicyName)]
    [InlineData(PresentationPolicyNames.SecretVerificationRateLimitPolicyName)]
    [InlineData(PresentationPolicyNames.BillingRateLimitPolicyName)]
    [InlineData(PresentationPolicyNames.ExportRateLimitPolicyName)]
    public void Configure_DedicatedPolicyFactory_CreatesPartition(string policyName) {
        var options = new RateLimiterOptions();
        new RateLimiterOptionsSetup(Microsoft.Extensions.Options.Options.Create(new ApiRateLimitingOptions())).Configure(options);
        object policy = FindPolicy(options, policyName);
        Delegate factory = Assert.Single(GetDelegates(policy));
        var httpContext = new DefaultHttpContext();
        httpContext.Connection.RemoteIpAddress = System.Net.IPAddress.Parse("198.51.100.25");

        object? partition = factory.DynamicInvoke(httpContext);

        Assert.NotNull(partition);
    }

    [Fact]
    public async Task Configure_OnRejected_WritesRetryAfterMetadata() {
        var options = new RateLimiterOptions();
        new RateLimiterOptionsSetup(Microsoft.Extensions.Options.Options.Create(new ApiRateLimitingOptions())).Configure(options);
        var httpContext = new DefaultHttpContext();
        httpContext.Response.Body = new MemoryStream();
        var context = new OnRejectedContext {
            HttpContext = httpContext,
            Lease = new RetryAfterLease(TimeSpan.FromMilliseconds(1200)),
        };

        await options.OnRejected!(context, CancellationToken.None);

        Assert.Multiple(
            () => Assert.Equal(StatusCodes.Status429TooManyRequests, httpContext.Response.StatusCode),
            () => Assert.Equal("2", httpContext.Response.Headers.RetryAfter));
    }

    private static string InvokeGetPartitionKey(HttpContext httpContext) {
        MethodInfo? method = typeof(RateLimiterOptionsSetup).GetMethod(
            "GetPartitionKey",
            BindingFlags.NonPublic | BindingFlags.Static);

        Assert.NotNull(method);
        object? result = method.Invoke(null, [httpContext]);
        return Assert.IsType<string>(result);
    }

    private static object FindPolicy(RateLimiterOptions options, string policyName) {
        IEnumerable<IDictionary> dictionaries = typeof(RateLimiterOptions)
            .GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            .Where(property => typeof(IDictionary).IsAssignableFrom(property.PropertyType))
            .Select(property => property.GetValue(options))
            .OfType<IDictionary>();
        IDictionary dictionary = Assert.Single(dictionaries, candidate => candidate.Contains(policyName));
        object? policy = dictionary[policyName];
        Assert.NotNull(policy);
        return policy;
    }

    private static DefaultHttpContext CreateWebhookContext(string provider, string remoteIpAddress) {
        var context = new DefaultHttpContext();
        context.Request.RouteValues["provider"] = provider;
        context.Connection.RemoteIpAddress = System.Net.IPAddress.Parse(remoteIpAddress);
        return context;
    }

    private static string GetPartitionKey(object? partition) {
        Assert.NotNull(partition);
        PropertyInfo? property = partition.GetType().GetProperty(
            "PartitionKey",
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        Assert.NotNull(property);
        object? value = property.GetValue(partition);
        if (value is string partitionKey) {
            return partitionKey;
        }

        Assert.NotNull(value);
        PropertyInfo? keyProperty = value.GetType().GetProperty(
            "Key",
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        Assert.NotNull(keyProperty);
        return Assert.IsType<string>(keyProperty.GetValue(value));
    }

    private static IReadOnlyList<Delegate> GetDelegates(object instance) {
        List<Delegate> delegates = [];
        for (Type? type = instance.GetType(); type is not null; type = type.BaseType) {
            delegates.AddRange(type
                .GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly)
                .Select(field => field.GetValue(instance))
                .OfType<Delegate>());
            delegates.AddRange(type
                .GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly)
                .Where(property => property.GetIndexParameters().Length == 0)
                .Select(property => property.GetValue(instance))
                .OfType<Delegate>());
        }

        return delegates;
    }

    [ExcludeFromCodeCoverage]
    private sealed class RetryAfterLease(TimeSpan retryAfter) : RateLimitLease {
        public override bool IsAcquired => false;

        public override IEnumerable<string> MetadataNames => [MetadataName.RetryAfter.Name];

        public override bool TryGetMetadata(string metadataName, out object? metadata) {
            if (string.Equals(metadataName, MetadataName.RetryAfter.Name, StringComparison.Ordinal)) {
                metadata = retryAfter;
                return true;
            }

            metadata = null;
            return false;
        }
    }
}
