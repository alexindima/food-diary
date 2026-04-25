using MediatR;

namespace FoodDiary.MailRelay.Application.Emails.Queries;

public sealed record GetMailRelaySuppressionsQuery(string? Email) : IRequest<IReadOnlyList<MailRelaySuppressionEntry>>;
