using FoodDiary.Domain.Entities.Users;

namespace FoodDiary.Application.Authentication.Common;

public interface IAuthenticationUserRegistrationService {
    Task<User> AddAsync(User user, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Role>> EnsureRolesByNamesAsync(IReadOnlyList<string> names, CancellationToken cancellationToken = default);
    Task<User?> GetByEmailIncludingDeletedAsync(string email, CancellationToken cancellationToken = default);
}
