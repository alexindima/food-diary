using FoodDiary.Modules.Users.Domain.Entities;

namespace FoodDiary.Modules.Users.Application.Abstractions.Common;

public interface IUserGoogleIdentityRepository {
    Task<User?> GetByGoogleIdentityIncludingDeletedAsync(
        string issuer,
        string subject,
        CancellationToken cancellationToken = default);
}
