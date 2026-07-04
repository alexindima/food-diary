namespace FoodDiary.Presentation.Api.Features.Version;

public interface IApiVersionInfo {
    string CommitSha { get; }
    string ImageTag { get; }
    string Environment { get; }
    string ApplicationVersion { get; }
    DateTimeOffset StartedAtUtc { get; }
}
