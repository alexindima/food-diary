using System.Diagnostics;
using FoodDiary.Web.Api.Extensions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Routing.Patterns;

namespace FoodDiary.Web.Api.Tests.Extensions;

[ExcludeFromCodeCoverage]
public sealed class TelemetryPrivacyProcessorTests {
    [Theory]
    [InlineData("/health", false)]
    [InlineData("/health/live", false)]
    [InlineData("/health/ready", false)]
    [InlineData("/api/v1/dashboard", true)]
    public void ShouldCollectRequest_ExcludesHealthEndpoints(string path, bool expected) {
        var context = new DefaultHttpContext();
        context.Request.Path = path;

        Assert.Equal(expected, TelemetryPrivacyProcessor.ShouldCollectRequest(context));
    }

    [Fact]
    public void ResolveRouteLabel_UsesTemplateInsteadOfConcretePath() {
        var context = new DefaultHttpContext();
        context.Request.Path = "/api/v1/users/2b495851-b83c-4ef0-a4cf-8cc96201098f";
        context.SetEndpoint(new RouteEndpoint(
            static _ => Task.CompletedTask,
            RoutePatternFactory.Parse("api/v1/users/{id:guid}"),
            order: 0,
            EndpointMetadataCollection.Empty,
            "user"));

        string route = TelemetryPrivacyProcessor.ResolveRouteLabel(context);

        Assert.Equal("/api/v1/users/{id:guid}", route);
    }

    [Fact]
    public void ResolveRouteLabel_WhenEndpointIsUnmatched_UsesFixedLabel() {
        var context = new DefaultHttpContext();
        context.Request.Path = "/attacker-controlled/9d3914a7";

        string route = TelemetryPrivacyProcessor.ResolveRouteLabel(context);

        Assert.Equal(TelemetryPrivacyProcessor.UnmatchedRouteLabel, route);
    }

    [Fact]
    public void Sanitize_RemovesSensitiveAttributesAndStatusDescription() {
        using Activity activity = new Activity("privacy-test").Start();
        activity.SetTag("url.full", "https://provider.example/api/items?access_token=secret");
        activity.SetTag("url.query", "access_token=secret");
        activity.SetTag("enduser.id", "user-123");
        activity.SetTag("http.request.header.authorization", "Bearer secret");
        activity.SetTag("http.response.header.set-cookie", "session=secret");
        activity.SetTag("http.request.body", "private request");
        activity.SetTag("provider.payload", "private response");
        activity.SetTag("db.query.text", "select * from users where email = 'private@example.test'");
        activity.SetTag("error.message", "private failure");
        activity.SetTag("error.type", "ProviderFailure");
        activity.SetStatus(ActivityStatusCode.Error, "private failure");

        TelemetryPrivacyProcessor.Sanitize(activity);

        Assert.Multiple(
            () => Assert.Equal("https://provider.example/api/items", activity.GetTagItem("url.full")),
            () => Assert.Null(activity.GetTagItem("url.query")),
            () => Assert.Null(activity.GetTagItem("enduser.id")),
            () => Assert.Null(activity.GetTagItem("http.request.header.authorization")),
            () => Assert.Null(activity.GetTagItem("http.response.header.set-cookie")),
            () => Assert.Null(activity.GetTagItem("http.request.body")),
            () => Assert.Null(activity.GetTagItem("provider.payload")),
            () => Assert.Null(activity.GetTagItem("db.query.text")),
            () => Assert.Null(activity.GetTagItem("error.message")),
            () => Assert.Equal("ProviderFailure", activity.GetTagItem("error.type")),
            () => Assert.Null(activity.StatusDescription));
    }

    [Fact]
    public void EnrichServerActivity_RemovesConcretePathAndQuery() {
        var context = new DefaultHttpContext();
        context.Request.Method = HttpMethods.Get;
        context.Request.Path = "/api/v1/users/2b495851-b83c-4ef0-a4cf-8cc96201098f";
        context.Request.QueryString = new QueryString("?token=secret");
        context.SetEndpoint(new RouteEndpoint(
            static _ => Task.CompletedTask,
            RoutePatternFactory.Parse("api/v1/users/{id:guid}"),
            order: 0,
            EndpointMetadataCollection.Empty,
            "user"));
        using Activity activity = new Activity("server-test").Start();
        activity.SetTag("url.path", context.Request.Path.Value);
        activity.SetTag("url.query", context.Request.QueryString.Value);

        TelemetryPrivacyProcessor.EnrichServerActivity(activity, context.Response);

        Assert.Multiple(
            () => Assert.Equal("GET /api/v1/users/{id:guid}", activity.DisplayName),
            () => Assert.Equal("/api/v1/users/{id:guid}", activity.GetTagItem("http.route")),
            () => Assert.Null(activity.GetTagItem("url.path")),
            () => Assert.Null(activity.GetTagItem("url.query")));
    }
}
