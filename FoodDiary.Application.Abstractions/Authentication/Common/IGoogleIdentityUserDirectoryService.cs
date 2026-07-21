using FoodDiary.Domain.Entities.Users;

namespace FoodDiary.Application.Abstractions.Authentication.Common;

public interface IGoogleIdentityUserDirectoryService {
    Task<User?> GetByGoogleIdentityIncludingDeletedAsync(
        string issuer,
        string subject,
        CancellationToken cancellationToken = default);
}
