using FoodDiary.Modules.Identity.Application.Abstractions.Authentication.Abstractions;
using System.Security.Cryptography;
using System.Globalization;
using FoodDiary.Authentication.Contracts.Authentication.Abstractions;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;

namespace FoodDiary.Modules.Identity.Infrastructure.Authentication;

public sealed class AdminSsoService(IAdminSsoCodeStore codeStore, TimeProvider dateTimeProvider) : IAdminSsoService {
    private static readonly TimeSpan CodeTtl = TimeSpan.FromMinutes(2);

    public async Task<AdminSsoCode> CreateCodeAsync(AdminSsoIssuer issuer, CancellationToken cancellationToken = default) {
        string code = GenerateCode();
        DateTime expiresAt = dateTimeProvider.GetUtcNow().UtcDateTime.Add(CodeTtl);

        await codeStore.StoreAsync(
            code,
            FormattableString.Invariant($"{issuer.UserId.Value:D}:{issuer.SessionId:D}:{issuer.SecurityVersion}"),
            CodeTtl,
            cancellationToken);

        return new AdminSsoCode(code, expiresAt);
    }

    public async Task<AdminSsoIssuer?> ExchangeCodeAsync(string code, CancellationToken cancellationToken = default) {
        if (!IsExpectedCode(code)) {
            return null;
        }

        string? value = await codeStore.ConsumeAsync(code, cancellationToken);
        string[]? fields = value?.Split(':');
        if (fields is not { Length: 3 } ||
            !Guid.TryParseExact(fields[0], "D", out Guid userId) || userId == Guid.Empty ||
            !Guid.TryParseExact(fields[1], "D", out Guid sessionId) || sessionId == Guid.Empty ||
            !long.TryParse(fields[2], NumberStyles.None, CultureInfo.InvariantCulture, out long securityVersion)) {
            return null;
        }

        return new AdminSsoIssuer(new UserId(userId), sessionId, securityVersion);
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
