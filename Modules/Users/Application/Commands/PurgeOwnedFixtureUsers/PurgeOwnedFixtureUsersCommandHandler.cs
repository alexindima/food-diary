using FoodDiary.Mediator;
using FoodDiary.Modules.Users.Application.Abstractions.Common;
using FoodDiary.Modules.Users.Contracts.Commands.PurgeOwnedFixtureUsers;

namespace FoodDiary.Modules.Users.Application.Commands.PurgeOwnedFixtureUsers;

public sealed class PurgeOwnedFixtureUsersCommandHandler(IUserCleanupService cleanup) : IRequestHandler<PurgeOwnedFixtureUsersCommand, int> {
    public Task<int> Handle(PurgeOwnedFixtureUsersCommand request, CancellationToken cancellationToken) =>
        cleanup.CleanupOwnedFixtureUsersAsync(request.RunId, request.UserIds, request.OlderThanUtc, cancellationToken);
}
