using FoodDiary.MailInbox.Application.Common.Result;
using FoodDiary.Mediator;

namespace FoodDiary.MailInbox.Application.Health;

public sealed record CheckMailInboxReadinessQuery() : IRequest<Result>;
