using FoodDiary.MailInbox.Application.Common.Results;
using FoodDiary.Mediator;

namespace FoodDiary.MailInbox.Application.Messages.Commands;

public sealed record MarkInboundMailMessageReadCommand(Guid Id) : IRequest<Result>;
