namespace FoodDiary.MailRelay.Presentation.Features.Email.Responses;

public sealed record EnqueuedMailRelayEmailHttpResponse(Guid Id, string Status);
