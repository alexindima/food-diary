using System.ComponentModel.DataAnnotations;

namespace FoodDiary.MailInbox.Presentation.Features.Messages.Requests;

public sealed record GetInboundMailMessagesHttpQuery(
    [Range(1, 200)] int? Limit = null,
    string? Recipient = null,
    string? Category = null,
    bool? Unread = null);
