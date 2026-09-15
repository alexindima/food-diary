namespace FoodDiary.Modules.Dietologist.Presentation.Requests;

public sealed record CreateClientTaskHttpRequest(
    string Title,
    string? Details,
    DateTime? DueAtUtc);
