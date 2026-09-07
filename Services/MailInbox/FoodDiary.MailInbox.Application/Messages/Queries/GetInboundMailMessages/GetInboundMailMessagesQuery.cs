using FoodDiary.Results;
using FoodDiary.MailInbox.Application.Messages.Models;
using FoodDiary.Mediator;

namespace FoodDiary.MailInbox.Application.Messages.Queries.GetInboundMailMessages;

public sealed record GetInboundMailMessagesQuery(int Limit) : IRequest<Result<IReadOnlyList<InboundMailMessageSummary>>>;
