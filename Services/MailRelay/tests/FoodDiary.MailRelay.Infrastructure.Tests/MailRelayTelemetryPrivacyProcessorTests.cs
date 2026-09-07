using System.Diagnostics;
using System.Reflection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Routing.Patterns;
using OpenTelemetry;
using FoodDiary.MailRelay.Infrastructure.Extensions;

namespace FoodDiary.MailRelay.Infrastructure.Tests;

[ExcludeFromCodeCoverage]
public sealed class MailRelayTelemetryPrivacyProcessorTests {
    private static readonly Type ProcessorType = typeof(MailRelayServiceCollectionExtensions).Assembly.GetType(
        "FoodDiary.MailRelay.Infrastructure.Extensions.MailRelayTelemetryPrivacyProcessor",
        throwOnError: true)!;

    [Fact]
    public void OnEnd_RemovesSensitiveTelemetryAndSanitizesUrl() {
        using Activity activity = new Activity("mail-relay-test").Start();
        activity.SetTag("url.full", "https://relay.example.test/api/email?api_key=secret#fragment");
        activity.SetTag("client.address", "192.0.2.10");
        activity.SetTag("message.body", "private body");
        activity.SetTag("provider.payload.value", "private payload");
        activity.SetTag("db.query.parameter.0", "private parameter");
        activity.SetTag("http.request.header.authorization", "secret");
        activity.SetTag("http.response.header.set-cookie", "secret");
        activity.SetTag("safe.tag", "safe-value");
        activity.SetStatus(ActivityStatusCode.Error, "private failure description");
        BaseProcessor<Activity> processor = CreateProcessor();

        processor.OnEnd(activity);

        Assert.Multiple(
            () => Assert.Equal("https://relay.example.test/api/email", activity.GetTagItem("url.full")),
            () => Assert.Null(activity.GetTagItem("client.address")),
            () => Assert.Null(activity.GetTagItem("message.body")),
            () => Assert.Null(activity.GetTagItem("provider.payload.value")),
            () => Assert.Null(activity.GetTagItem("db.query.parameter.0")),
            () => Assert.Null(activity.GetTagItem("http.request.header.authorization")),
            () => Assert.Null(activity.GetTagItem("http.response.header.set-cookie")),
            () => Assert.Equal("safe-value", activity.GetTagItem("safe.tag")),
            () => Assert.Equal(ActivityStatusCode.Error, activity.Status),
            () => Assert.Null(activity.StatusDescription));
    }

    [Fact]
    public void OnEnd_WhenUrlIsInvalid_RemovesUrl() {
        using Activity activity = new Activity("mail-relay-test").Start();
        activity.SetTag("url.full", "not an absolute URI");

        CreateProcessor().OnEnd(activity);

        Assert.Null(activity.GetTagItem("url.full"));
    }

    [Theory]
    [InlineData("/health", false)]
    [InlineData("/HEALTH/ready", false)]
    [InlineData("/api/email", true)]
    public void ShouldCollectRequest_ReturnsExpectedResult(string path, bool expected) {
        var context = new DefaultHttpContext();
        context.Request.Path = path;

        bool result = InvokeStatic<bool>("ShouldCollectRequest", context);

        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("api/email/{id}", "/api/email/{id}")]
    [InlineData("/api/email/{id}", "/api/email/{id}")]
    public void EnrichServerActivity_UsesSanitizedRoute(string routePattern, string expectedRoute) {
        using Activity activity = new Activity("mail-relay-test").Start();
        var context = new DefaultHttpContext();
        context.Request.Method = HttpMethods.Post;
        context.Request.Path = "/api/email/private-value";
        context.Request.QueryString = new QueryString("?api_key=secret");
        context.SetEndpoint(new RouteEndpoint(
            _ => Task.CompletedTask,
            RoutePatternFactory.Parse(routePattern),
            order: 0,
            EndpointMetadataCollection.Empty,
            displayName: null));

        InvokeStatic("EnrichServerActivity", activity, context.Response);

        Assert.Multiple(
            () => Assert.Equal($"POST {expectedRoute}", activity.DisplayName),
            () => Assert.Equal(expectedRoute, activity.GetTagItem("http.route")),
            () => Assert.Null(activity.GetTagItem("url.path")),
            () => Assert.Null(activity.GetTagItem("url.query")));
    }

    [Fact]
    public void EnrichServerActivity_WhenEndpointIsMissing_UsesUnmatchedRoute() {
        using Activity activity = new Activity("mail-relay-test").Start();
        var context = new DefaultHttpContext();
        context.Request.Method = HttpMethods.Get;

        InvokeStatic("EnrichServerActivity", activity, context.Response);

        Assert.Equal("GET unmatched", activity.DisplayName);
        Assert.Equal("unmatched", activity.GetTagItem("http.route"));
    }

    [Fact]
    public void EnrichClientActivity_WithRequest_RemovesQueryAndFragment() {
        using Activity activity = new Activity("mail-relay-test").Start();
        using var request = new HttpRequestMessage(
            HttpMethod.Get,
            "https://provider.example.test/send/path?token=secret#fragment");

        InvokeStatic("EnrichClientActivity", activity, request);

        Assert.Equal("https://provider.example.test/send/path", activity.GetTagItem("url.full"));
    }

    [Fact]
    public void EnrichClientActivity_WithResponseWithoutRequest_RemovesUrl() {
        using Activity activity = new Activity("mail-relay-test").Start();
        activity.SetTag("url.full", "https://provider.example.test/private");
        using var response = new HttpResponseMessage();

        InvokeStatic("EnrichClientActivity", activity, response);

        Assert.Null(activity.GetTagItem("url.full"));
    }

    private static BaseProcessor<Activity> CreateProcessor() =>
        (BaseProcessor<Activity>)Activator.CreateInstance(ProcessorType, nonPublic: true)!;

    private static T InvokeStatic<T>(string methodName, params object[] arguments) =>
        (T)InvokeStatic(methodName, arguments)!;

    private static object? InvokeStatic(string methodName, params object[] arguments) {
        MethodInfo method = ProcessorType.GetMethods(BindingFlags.Public | BindingFlags.Static)
            .Single(candidate =>
                string.Equals(candidate.Name, methodName, StringComparison.Ordinal) &&
                candidate.GetParameters().Select(static parameter => parameter.ParameterType)
                    .Zip(arguments.Select(static argument => argument.GetType()))
                    .All(static pair => pair.First.IsAssignableFrom(pair.Second)));
        return method.Invoke(obj: null, arguments);
    }
}
