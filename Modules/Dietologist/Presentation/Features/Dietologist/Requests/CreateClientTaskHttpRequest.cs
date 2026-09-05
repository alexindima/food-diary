namespace FoodDiary.Presentation.Api.Features.Dietologist.Requests;

public sealed record CreateClientTaskHttpRequest(
    string Title,
    string? Details,
    DateTime? DueAtUtc);
