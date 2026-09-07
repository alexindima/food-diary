using FoodDiary.Mediator;

namespace FoodDiary.MailRelay.Application.Emails.Commands.EnqueueMailRelayEmail;

public sealed record EnqueueMailRelayEmailCommand(RelayEmailMessageRequest Request) : IRequest<Result<Guid>>;
