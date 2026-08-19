using FoodDiary.Results;
using FoodDiary.MailInbox.Application.Messages.Models;
using FoodDiary.Mediator;

namespace FoodDiary.MailInbox.Application.Messages.Commands.ReceiveInboundMail;

public sealed record ReceiveInboundMailCommand(ReceiveInboundMailRequest Request) : IRequest<Result<Guid>>;
