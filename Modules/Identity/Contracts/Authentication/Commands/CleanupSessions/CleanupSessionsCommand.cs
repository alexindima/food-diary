using FoodDiary.Mediator;

namespace FoodDiary.Modules.Identity.Contracts.Authentication.Commands.CleanupSessions;

public sealed record CleanupSessionsCommand(DateTime OlderThanUtc, int BatchSize, int MaxBatches = 20) : IRequest<int>;
