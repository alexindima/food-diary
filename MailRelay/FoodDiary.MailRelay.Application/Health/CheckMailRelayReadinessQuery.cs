using FoodDiary.Mediator;

namespace FoodDiary.MailRelay.Application.Health;

public sealed record CheckMailRelayReadinessQuery() : IRequest<Result>;
