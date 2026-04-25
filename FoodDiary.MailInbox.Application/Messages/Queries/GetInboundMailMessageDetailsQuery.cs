using FoodDiary.MailInbox.Application.Common.Result;
using FoodDiary.MailInbox.Application.Messages.Models;
using MediatR;

namespace FoodDiary.MailInbox.Application.Messages.Queries;

public sealed record GetInboundMailMessageDetailsQuery(Guid Id) : IRequest<Result<InboundMailMessageDetails>>;
