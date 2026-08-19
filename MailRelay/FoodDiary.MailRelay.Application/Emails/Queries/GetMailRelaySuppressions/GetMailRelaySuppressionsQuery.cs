using FoodDiary.Mediator;

namespace FoodDiary.MailRelay.Application.Emails.Queries.GetMailRelaySuppressions;

public sealed record GetMailRelaySuppressionsQuery(string? Email) : IRequest<Result<IReadOnlyList<MailRelaySuppressionEntry>>>;
