using FoodDiary.Mediator;

namespace FoodDiary.Modules.Users.Contracts.Commands.CleanupDeletedUsers;

public sealed record CleanupDeletedUsersCommand(DateTime OlderThanUtc, int BatchSize, Guid? ReassignUserId) : IRequest<int>;
