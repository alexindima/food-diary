namespace FoodDiary.Web.Api.Build;

public sealed record ApiVersionResponse(
    string CommitSha,
    string ImageTag,
    string Environment,
    string ApplicationVersion,
    DateTimeOffset StartedAtUtc);
