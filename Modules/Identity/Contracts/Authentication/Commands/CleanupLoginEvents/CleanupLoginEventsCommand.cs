using FoodDiary.Mediator;
namespace FoodDiary.Modules.Identity.Contracts.Authentication.Commands.CleanupLoginEvents;
public sealed record CleanupLoginEventsCommand(DateTime OlderThanUtc, int BatchSize) : IRequest<int>;
