using FoodDiary.Domain.Entities.Users;

namespace FoodDiary.Application.Abstractions.Users.Common;

public interface IUserGoogleIdentityRepository {
    Task<User?> GetByGoogleIdentityIncludingDeletedAsync(
        string issuer,
        string subject,
        CancellationToken cancellationToken = default);
}
