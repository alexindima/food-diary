using System.ComponentModel.DataAnnotations;

namespace FoodDiary.MailRelay.Presentation.Features.Email.Requests;

public sealed record GetMailRelayCollectionHttpQuery(
    string? Email = null,
    [Range(1, 10_000)] int Page = 1,
    [Range(1, 100)] int Limit = 20);
