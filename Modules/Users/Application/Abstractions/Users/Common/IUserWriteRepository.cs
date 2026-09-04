using FoodDiary.Domain.Entities.Users;

namespace FoodDiary.Application.Abstractions.Users.Common;

public interface IUserWriteRepository {
    Task<User> AddAsync(User user, CancellationToken cancellationToken = default);
    Task UpdateAsync(User user, CancellationToken cancellationToken = default);

    Task UpdateAsync(
        User user,
        IReadOnlyCollection<UserRoleAuditEvent> roleAuditEvents,
        CancellationToken cancellationToken = default) =>
        UpdateAsync(user, cancellationToken);
}
