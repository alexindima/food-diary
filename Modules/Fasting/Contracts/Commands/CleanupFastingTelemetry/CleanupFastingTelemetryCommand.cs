using FoodDiary.Mediator;

namespace FoodDiary.Modules.Fasting.Contracts.Commands.CleanupFastingTelemetry;

// Trusted owner operation: the caller authorizes the supplied scope.
public sealed record CleanupFastingTelemetryCommand(DateTime OlderThanUtc, int BatchSize) : IRequest<int>;
