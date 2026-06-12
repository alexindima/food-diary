namespace FoodDiary.MailRelay.Presentation.Features.Email.Requests;

public sealed record AwsSesMailHttpRequest(
    string? MessageId,
    IReadOnlyList<string> Destination);
