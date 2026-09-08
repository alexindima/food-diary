using FoodDiary.MailInbox.Application.Messages.Models;
using FoodDiary.Mediator;
using FoodDiary.Results;

namespace FoodDiary.MailInbox.Application.Messages.Queries.GetInboundMailMessagePage;

public sealed record GetInboundMailMessagePageQuery(int Page = 1, int Limit = 50, string? Recipient = null, string? Category = null, bool? Unread = null)
    : IRequest<Result<InboundMailMessagePage>>;

