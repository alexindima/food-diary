using FoodDiary.MailInbox.Application.Common.Result;
using FoodDiary.MailInbox.Application.Messages.Models;
using FoodDiary.Mediator;

namespace FoodDiary.MailInbox.Application.Messages.Queries;

public sealed record GetInboundMailMessageDetailsQuery(Guid Id) : IRequest<Result<InboundMailMessageDetails>>;
