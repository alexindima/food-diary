using FoodDiary.Mediator;

namespace FoodDiary.MailRelay.Application.Emails.Commands;

public sealed record EnqueueMailRelayEmailCommand(RelayEmailMessageRequest Request) : IRequest<Result<Guid>>;
