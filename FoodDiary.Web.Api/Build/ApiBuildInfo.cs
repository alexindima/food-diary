using System.Reflection;
using FoodDiary.Web.Api.Options;

namespace FoodDiary.Web.Api.Build;

public sealed record ApiBuildInfo(
    string CommitSha,
    string ImageTag,
    string Environment,
    string ApplicationVersion,
    DateTimeOffset StartedAtUtc) {
    public static ApiBuildInfo Create(ApiBuildInfoOptions options, string environmentName) {
        var applicationVersion = typeof(Program).Assembly
                                     .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
                                     .InformationalVersion
                                 ?? typeof(Program).Assembly.GetName().Version?.ToString()
                                 ?? "unknown";

        return new ApiBuildInfo(
            string.IsNullOrWhiteSpace(options.CommitSha) ? "unknown" : options.CommitSha,
            string.IsNullOrWhiteSpace(options.ImageTag) ? "unknown" : options.ImageTag,
            string.IsNullOrWhiteSpace(environmentName) ? "unknown" : environmentName,
            applicationVersion,
            DateTimeOffset.UtcNow);
    }
}
