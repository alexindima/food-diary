using System.Security.Cryptography;
using FoodDiary.Application.Abstractions.Admin.Common;
using FoodDiary.Application.Abstractions.Authentication.Abstractions;

namespace FoodDiary.Infrastructure.Authentication;

public sealed class AdminImpersonationHandoffService(IAdminSsoCodeStore codeStore) : IAdminImpersonationHandoffService {
    private static readonly TimeSpan CodeTtl = TimeSpan.FromMinutes(2);
    private const string CodePrefix = "imp_";
    private const string ValuePrefix = "impersonation:";

    public async Task<string> CreateCodeAsync(string accessToken, CancellationToken cancellationToken = default) {
        ArgumentException.ThrowIfNullOrWhiteSpace(accessToken);
        string code = GenerateCode();
        await codeStore.StoreAsync(code, ValuePrefix + accessToken, CodeTtl, cancellationToken).ConfigureAwait(false);
        return code;
    }

    public async Task<string?> ConsumeCodeAsync(string code, CancellationToken cancellationToken = default) {
        if (string.IsNullOrWhiteSpace(code)) {
            return null;
        }

        string normalizedCode = code.Trim();
        if (!normalizedCode.StartsWith(CodePrefix, StringComparison.Ordinal)) {
            return null;
        }

        string? value = await codeStore.ConsumeAsync(normalizedCode, cancellationToken).ConfigureAwait(false);
        return value?.StartsWith(ValuePrefix, StringComparison.Ordinal) == true
            ? value[ValuePrefix.Length..]
            : null;
    }

    private static string GenerateCode() => CodePrefix + Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))
        .TrimEnd('=')
        .Replace('+', '-')
        .Replace('/', '_');
}
