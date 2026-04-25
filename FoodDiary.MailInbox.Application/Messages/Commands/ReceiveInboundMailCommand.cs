using FoodDiary.MailInbox.Application.Common.Result;
using FoodDiary.MailInbox.Application.Messages.Models;
using MediatR;

namespace FoodDiary.MailInbox.Application.Messages.Commands;

public sealed record ReceiveInboundMailCommand(ReceiveInboundMailRequest Request) : IRequest<Result<Guid>>;
