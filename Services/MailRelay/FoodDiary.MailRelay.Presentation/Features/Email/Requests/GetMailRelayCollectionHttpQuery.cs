namespace FoodDiary.MailRelay.Presentation.Features.Email.Requests;

public sealed record GetMailRelayCollectionHttpQuery(string? Email = null, int Page = 1, int Limit = 20);
