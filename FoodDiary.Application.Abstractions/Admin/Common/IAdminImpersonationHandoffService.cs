namespace FoodDiary.Application.Abstractions.Admin.Common;

public interface IAdminImpersonationHandoffService {
    Task<string> CreateCodeAsync(string accessToken, CancellationToken cancellationToken = default);

    Task<string?> ConsumeCodeAsync(string code, CancellationToken cancellationToken = default);
}
