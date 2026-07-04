namespace FoodDiary.Presentation.Api.Features.Version.Responses;

public sealed record ApiVersionHttpResponse(
    string CommitSha,
    string ImageTag,
    string Environment,
    string ApplicationVersion,
    DateTimeOffset StartedAtUtc);
