namespace FoodDiary.MailRelay.Application.Emails.Models;

public sealed record MailRelayPage<T>(IReadOnlyList<T> Data, int Page, int Limit, int TotalPages, int TotalItems);
