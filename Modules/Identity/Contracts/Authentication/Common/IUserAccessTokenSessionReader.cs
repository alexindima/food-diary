namespace FoodDiary.Modules.Identity.Contracts.Authentication.Common;

public interface IUserAccessTokenSessionReader {
    Task<bool> IsActiveAsync(Guid userId, Guid sessionId, CancellationToken cancellationToken = default);
}
