using FoodDiary.Mediator;

namespace FoodDiary.Modules.Marketing.Contracts.Commands.RecordPremiumConversion;

// Executes within the caller-owned unit of work; this request does not commit.
public sealed record RecordPremiumConversionCommand(
    Guid UserId) : IRequest<Unit>;
