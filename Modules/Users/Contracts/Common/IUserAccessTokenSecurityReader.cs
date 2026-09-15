namespace FoodDiary.Modules.Users.Contracts.Common;

public interface IUserAccessTokenSecurityReader {
    Task<bool> IsCurrentAsync(
        Guid userId,
        long securityVersion,
        CancellationToken cancellationToken = default);
}
