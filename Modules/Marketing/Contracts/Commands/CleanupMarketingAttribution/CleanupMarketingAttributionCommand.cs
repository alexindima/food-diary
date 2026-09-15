using FoodDiary.Mediator;

namespace FoodDiary.Modules.Marketing.Contracts.Commands.CleanupMarketingAttribution;

public sealed record CleanupMarketingAttributionCommand(DateTime OlderThanUtc, int BatchSize) : IRequest<int>;
