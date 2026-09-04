namespace FoodDiary.Application.Abstractions.Users.Common;

public interface IUserAccessTokenSecurityReader {
    Task<bool> IsCurrentAsync(
        Guid userId,
        long securityVersion,
        CancellationToken cancellationToken = default);
}
