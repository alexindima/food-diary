namespace FoodDiary.Application.Abstractions.Authentication.Abstractions;

public interface IAdminSsoCodeStore {
    Task StoreAsync(
        string code,
        string userId,
        TimeSpan lifetime,
        CancellationToken cancellationToken = default);

    Task<string?> ConsumeAsync(string code, CancellationToken cancellationToken = default);
}
