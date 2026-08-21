using System.Diagnostics;
using System.Reflection;
using FoodDiary.JobManager.Services;
using OpenTelemetry;

namespace FoodDiary.JobManager.Tests;

[ExcludeFromCodeCoverage]
public sealed class TelemetryPrivacyProcessorTests {
    private static readonly Type ProcessorType = typeof(JobManagerTelemetryServiceCollectionExtensions).Assembly.GetType(
        "FoodDiary.JobManager.Services.TelemetryPrivacyProcessor",
        throwOnError: true)!;

    [Fact]
    public void OnEnd_RemovesSensitiveTelemetryAndSanitizesUrl() {
        using Activity activity = new Activity("job-manager-test").Start();
        activity.SetTag("url.full", "https://provider.example.test/jobs/run?token=secret#fragment");
        activity.SetTag("db.statement", "select private_data");
        activity.SetTag("message.body", "private body");
        activity.SetTag("provider.payload", "private payload");
        activity.SetTag("db.query.parameter.0", "private parameter");
        activity.SetTag("gen_ai.input.messages", "private prompt");
        activity.SetTag("gen_ai.output.messages", "private response");
        activity.SetTag("http.request.header.authorization", "secret");
        activity.SetTag("http.response.header.set-cookie", "secret");
        activity.SetTag("safe.tag", "safe-value");
        activity.SetStatus(ActivityStatusCode.Error, "private failure description");

        CreateProcessor().OnEnd(activity);

        Assert.Multiple(
            () => Assert.Equal("https://provider.example.test/jobs/run", activity.GetTagItem("url.full")),
            () => Assert.Null(activity.GetTagItem("db.statement")),
            () => Assert.Null(activity.GetTagItem("message.body")),
            () => Assert.Null(activity.GetTagItem("provider.payload")),
            () => Assert.Null(activity.GetTagItem("db.query.parameter.0")),
            () => Assert.Null(activity.GetTagItem("gen_ai.input.messages")),
            () => Assert.Null(activity.GetTagItem("gen_ai.output.messages")),
            () => Assert.Null(activity.GetTagItem("http.request.header.authorization")),
            () => Assert.Null(activity.GetTagItem("http.response.header.set-cookie")),
            () => Assert.Equal("safe-value", activity.GetTagItem("safe.tag")),
            () => Assert.Null(activity.StatusDescription));
    }

    [Fact]
    public void OnEnd_WhenUrlIsInvalid_RemovesUrl() {
        using Activity activity = new Activity("job-manager-test").Start();
        activity.SetTag("url.full", "not an absolute URI");

        CreateProcessor().OnEnd(activity);

        Assert.Null(activity.GetTagItem("url.full"));
    }

    [Fact]
    public void EnrichClientActivity_WithRequest_RemovesQueryAndFragment() {
        using Activity activity = new Activity("job-manager-test").Start();
        using var request = new HttpRequestMessage(
            HttpMethod.Get,
            "https://provider.example.test/jobs?token=secret#fragment");

        InvokeStatic("EnrichClientActivity", activity, request);

        Assert.Equal("https://provider.example.test/jobs", activity.GetTagItem("url.full"));
    }

    [Fact]
    public void EnrichClientActivity_WithResponseWithoutRequest_RemovesUrl() {
        using Activity activity = new Activity("job-manager-test").Start();
        activity.SetTag("url.full", "https://provider.example.test/private");
        using var response = new HttpResponseMessage();

        InvokeStatic("EnrichClientActivity", activity, response);

        Assert.Null(activity.GetTagItem("url.full"));
    }

    private static BaseProcessor<Activity> CreateProcessor() =>
        (BaseProcessor<Activity>)Activator.CreateInstance(ProcessorType, nonPublic: true)!;

    private static void InvokeStatic(string methodName, params object[] arguments) {
        MethodInfo method = ProcessorType.GetMethods(BindingFlags.Public | BindingFlags.Static)
            .Single(candidate =>
                string.Equals(candidate.Name, methodName, StringComparison.Ordinal) &&
                candidate.GetParameters().Select(static parameter => parameter.ParameterType)
                    .Zip(arguments.Select(static argument => argument.GetType()))
                    .All(static pair => pair.First.IsAssignableFrom(pair.Second)));
        _ = method.Invoke(obj: null, arguments);
    }
}
