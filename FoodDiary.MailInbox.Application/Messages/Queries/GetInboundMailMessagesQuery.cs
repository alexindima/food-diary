using FoodDiary.MailInbox.Application.Common.Result;
using FoodDiary.MailInbox.Application.Messages.Models;
using FoodDiary.Mediator;

namespace FoodDiary.MailInbox.Application.Messages.Queries;

public sealed record GetInboundMailMessagesQuery(int Limit) : IRequest<Result<IReadOnlyList<InboundMailMessageSummary>>>;
