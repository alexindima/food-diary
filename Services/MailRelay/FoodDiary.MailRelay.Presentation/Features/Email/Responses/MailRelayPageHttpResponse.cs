namespace FoodDiary.MailRelay.Presentation.Features.Email.Responses;

public sealed record MailRelayPageHttpResponse<T>(IReadOnlyList<T> Data, int Page, int Limit, int TotalPages, int TotalItems);
