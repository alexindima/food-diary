namespace FoodDiary.Web.Api.Options;

public sealed class ApiBuildInfoOptions {
    public const string SectionName = "BuildInfo";

    public string? CommitSha { get; init; }

    public string? ImageTag { get; init; }

    public static bool HasValidCommitSha(ApiBuildInfoOptions options) {
        return string.IsNullOrWhiteSpace(options.CommitSha) || options.CommitSha.Length <= 128;
    }

    public static bool HasValidImageTag(ApiBuildInfoOptions options) {
        return string.IsNullOrWhiteSpace(options.ImageTag) || options.ImageTag.Length <= 256;
    }
}
