using MediatR;

namespace FoodDiary.MailRelay.Application.Health;

public sealed record CheckMailRelayReadinessQuery() : IRequest;
