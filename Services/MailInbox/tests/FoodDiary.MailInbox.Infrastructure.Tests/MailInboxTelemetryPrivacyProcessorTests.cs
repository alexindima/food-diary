using System.Diagnostics;
using System.Reflection;
using FoodDiary.MailInbox.Infrastructure.Extensions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Routing.Patterns;

namespace FoodDiary.MailInbox.Infrastructure.Tests;

[ExcludeFromCodeCoverage]
public sealed class MailInboxTelemetryPrivacyProcessorTests {
    private static readonly Type ProcessorType = typeof(MailInboxServiceCollectionExtensions).Assembly.GetType(
        "FoodDiary.MailInbox.Infrastructure.Extensions.MailInboxTelemetryPrivacyProcessor",
        throwOnError: true)!;

    [Fact]
    public void OnEnd_RemovesEverySensitiveTagShapeAndErrorDescription() {
        object processor = Activator.CreateInstance(ProcessorType, nonPublic: true)!;
        using var activity = new Activity("privacy-test");
        activity.Start();
        string[] sensitiveTags = [
            "client.address",
            "request.body",
            "custom.PayloadData",
            "db.query.parameter.0",
            "http.request.header.authorization",
            "http.response.header.set-cookie",
        ];
        foreach (string tag in sensitiveTags) {
            activity.SetTag(tag, "secret");
        }

        activity.SetTag("safe.tag", "safe");
        activity.SetStatus(ActivityStatusCode.Error, "secret failure");

        Invoke("OnEnd", processor, activity);

        Assert.All(sensitiveTags, tag => Assert.Null(activity.GetTagItem(tag)));
        Assert.Equal("safe", activity.GetTagItem("safe.tag"));
        Assert.Equal(ActivityStatusCode.Error, activity.Status);
        Assert.Null(activity.StatusDescription);
    }

    [Fact]
    public void OnEnd_WhenActivityIsNotAnError_PreservesStatus() {
        object processor = Activator.CreateInstance(ProcessorType, nonPublic: true)!;
        using var activity = new Activity("privacy-control");
        activity.Start();
        activity.SetStatus(ActivityStatusCode.Ok);

        Invoke("OnEnd", processor, activity);

        Assert.Equal(ActivityStatusCode.Ok, activity.Status);
    }

    [Theory]
    [InlineData("/health", false)]
    [InlineData("/health/ready", false)]
    [InlineData("/messages", true)]
    public void ShouldCollectRequest_ReturnsExpectedResult(string path, bool expected) {
        var context = new DefaultHttpContext();
        context.Request.Path = path;

        bool result = (bool)Invoke("ShouldCollectRequest", instance: null, context)!;

        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("messages/{id}", "/messages/{id}")]
    [InlineData("/messages/{id}", "/messages/{id}")]
    [InlineData("", "")]
    public void EnrichServerActivity_UsesRoutePatternWithoutRequestValues(string pattern, string expectedRoute) {
        using var activity = new Activity("request");
        activity.Start();
        activity.SetTag("url.path", "/messages/private-id");
        activity.SetTag("url.query", "token=secret");
        var context = new DefaultHttpContext();
        context.Request.Method = "GET";
        var endpoint = new RouteEndpoint(
            static _ => Task.CompletedTask,
            RoutePatternFactory.Parse(pattern),
            order: 0,
            EndpointMetadataCollection.Empty,
            displayName: "test");
        context.SetEndpoint(endpoint);

        Invoke("EnrichServerActivity", instance: null, activity, context.Response);

        Assert.Equal($"GET {expectedRoute}", activity.DisplayName);
        Assert.Equal(expectedRoute, activity.GetTagItem("http.route"));
        Assert.Null(activity.GetTagItem("url.path"));
        Assert.Null(activity.GetTagItem("url.query"));
    }

    [Fact]
    public void EnrichServerActivity_WhenRouteIsUnavailable_UsesUnmatched() {
        using var activity = new Activity("request");
        activity.Start();
        var context = new DefaultHttpContext();
        context.Request.Method = "POST";

        Invoke("EnrichServerActivity", instance: null, activity, context.Response);

        Assert.Equal("POST unmatched", activity.DisplayName);
        Assert.Equal("unmatched", activity.GetTagItem("http.route"));
    }

    private static object? Invoke(string methodName, object? instance, params object?[] parameters) {
        MethodInfo method = ProcessorType.GetMethod(
            methodName,
            BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static)!
            ?? throw new InvalidOperationException($"Method {methodName} was not found.");
        return method.Invoke(instance, parameters);
    }
}
