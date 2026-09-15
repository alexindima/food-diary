using FoodDiary.Modules.Users.Domain.Entities;

namespace FoodDiary.Modules.Users.Application.Abstractions.Common;

public interface IUserWriteRepository {
    Task<User> AddAsync(User user, CancellationToken cancellationToken = default);
    Task UpdateAsync(User user, CancellationToken cancellationToken = default);

    Task UpdateAsync(
        User user,
        IReadOnlyCollection<UserRoleAuditEvent> roleAuditEvents,
        CancellationToken cancellationToken = default) =>
        UpdateAsync(user, cancellationToken);
}
