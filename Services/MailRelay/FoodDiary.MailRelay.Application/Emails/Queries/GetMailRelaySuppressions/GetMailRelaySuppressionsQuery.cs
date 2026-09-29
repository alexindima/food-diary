using FoodDiary.Mediator;

namespace FoodDiary.MailRelay.Application.Emails.Queries.GetMailRelaySuppressions;

public sealed record GetMailRelaySuppressionsQuery(string? Email, int Page = 1, int Limit = 20) : IRequest<Result<MailRelayPage<MailRelaySuppressionEntry>>>;
