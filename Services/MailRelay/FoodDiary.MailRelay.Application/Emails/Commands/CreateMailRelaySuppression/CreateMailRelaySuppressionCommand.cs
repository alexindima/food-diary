using FoodDiary.Mediator;

namespace FoodDiary.MailRelay.Application.Emails.Commands.CreateMailRelaySuppression;

public sealed record CreateMailRelaySuppressionCommand(CreateSuppressionRequest Request) : IRequest<Result>;
