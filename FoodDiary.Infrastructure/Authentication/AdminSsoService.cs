using System.Security.Cryptography;
using FoodDiary.Application.Abstractions.Authentication.Abstractions;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Infrastructure.Authentication;

public sealed class AdminSsoService(IAdminSsoCodeStore codeStore, TimeProvider dateTimeProvider) : IAdminSsoService {
    private static readonly TimeSpan CodeTtl = TimeSpan.FromMinutes(2);

    public async Task<AdminSsoCode> CreateCodeAsync(UserId userId, CancellationToken cancellationToken = default) {
        string code = GenerateCode();
        DateTime expiresAt = dateTimeProvider.GetUtcNow().UtcDateTime.Add(CodeTtl);

        await codeStore.StoreAsync(
            code,
            userId.Value.ToString(),
            CodeTtl,
            cancellationToken).ConfigureAwait(false);

        return new AdminSsoCode(code, expiresAt);
    }

    public async Task<UserId?> ExchangeCodeAsync(string code, CancellationToken cancellationToken = default) {
        if (!IsExpectedCode(code)) {
            return null;
        }

        string? value = await codeStore.ConsumeAsync(code, cancellationToken).ConfigureAwait(false);
        if (string.IsNullOrWhiteSpace(value)) {
            return null;
        }

        return Guid.TryParse(value, out Guid id) ? new UserId(id) : null;
    }

    private static bool IsExpectedCode(string code) =>
        !string.IsNullOrWhiteSpace(code) &&
        code.Length == 43 &&
        code.All(static character => char.IsAsciiLetterOrDigit(character) || character is '-' or '_');

    private static string GenerateCode() {
        byte[] bytes = RandomNumberGenerator.GetBytes(32);
        return Convert.ToBase64String(bytes)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }
}
