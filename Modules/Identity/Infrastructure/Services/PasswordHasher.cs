using FoodDiary.Application.Abstractions.Authentication.Common;

namespace FoodDiary.Infrastructure.Services;

public sealed class PasswordHasher : IPasswordHasher {
    private const string EnhancedHashPrefix = "$fd$bcrypt-sha384$";

    public string Hash(string password) =>
        EnhancedHashPrefix + BCrypt.Net.BCrypt.EnhancedHashPassword(password, BCrypt.Net.HashType.SHA384);

    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
    public bool Verify(string password, string hashedPassword) {
        if (string.IsNullOrEmpty(password) || string.IsNullOrWhiteSpace(hashedPassword)) {
            return false;
        }

        try {
            return hashedPassword.StartsWith(EnhancedHashPrefix, StringComparison.Ordinal)
                ? BCrypt.Net.BCrypt.EnhancedVerify(
                    password,
                    hashedPassword[EnhancedHashPrefix.Length..],
                    BCrypt.Net.HashType.SHA384)
                : BCrypt.Net.BCrypt.Verify(password, hashedPassword);
        } catch (BCrypt.Net.SaltParseException) {
            return false;
        } catch (ArgumentException) {
            return false;
        }
    }

    public bool NeedsRehash(string hashedPassword) =>
        !hashedPassword.StartsWith(EnhancedHashPrefix, StringComparison.Ordinal);
}
