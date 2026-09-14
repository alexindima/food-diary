using FoodDiary.Mediator;

namespace FoodDiary.Application.Marketing.Commands.RecordPremiumConversion;

// Executes within the caller-owned unit of work; this request does not commit.
public sealed record RecordPremiumConversionCommand(
    Guid UserId) : IRequest<Unit>;
