using MediatR;

namespace FoodDiary.MailRelay.Application.Emails.Commands;

public sealed record CreateMailRelaySuppressionCommand(CreateSuppressionRequest Request) : IRequest<Result>;
