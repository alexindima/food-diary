using FoodDiary.Mediator;

namespace FoodDiary.Modules.Admin.Contracts.Commands.SendBugAcknowledgements;

// Email dispatch and per-message receipt persistence must not be replayed as one transaction.
public sealed record SendBugAcknowledgementsCommand(DateTimeOffset Since) : IRequest<Unit>;
