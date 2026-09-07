using FoodDiary.Mediator;

namespace FoodDiary.MailRelay.Application.Emails.Commands.RemoveMailRelaySuppression;

public sealed record RemoveMailRelaySuppressionCommand(string Email) : IRequest<Result>;
