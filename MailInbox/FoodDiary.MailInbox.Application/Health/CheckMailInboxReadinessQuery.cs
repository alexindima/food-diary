using FoodDiary.Results;
using FoodDiary.Mediator;

namespace FoodDiary.MailInbox.Application.Health;

public sealed record CheckMailInboxReadinessQuery() : IRequest<Result>;
