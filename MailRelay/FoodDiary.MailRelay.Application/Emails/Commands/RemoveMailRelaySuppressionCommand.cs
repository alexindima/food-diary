using FoodDiary.Mediator;

namespace FoodDiary.MailRelay.Application.Emails.Commands;

public sealed record RemoveMailRelaySuppressionCommand(string Email) : IRequest<Result>;
