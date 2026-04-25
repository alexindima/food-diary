using FoodDiary.MailInbox.Application.Common.Result;
using MediatR;

namespace FoodDiary.MailInbox.Application.Health;

public sealed record CheckMailInboxReadinessQuery() : IRequest<Result>;
