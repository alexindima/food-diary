using FoodDiary.Mediator;

namespace FoodDiary.Modules.Users.Contracts.Commands.PurgeOwnedFixtureUsers;

/// <summary>Explicit synthetic fixture scope; never scans the general deleted-user queue.</summary>
public sealed record PurgeOwnedFixtureUsersCommand(Guid RunId, IReadOnlyList<Guid> UserIds, DateTime OlderThanUtc) : IRequest<int>;
