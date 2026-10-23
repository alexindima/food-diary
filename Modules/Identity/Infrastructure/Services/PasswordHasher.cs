using BCrypt.Net;
using System.Diagnostics.CodeAnalysis;
using BCryptHasher = BCrypt.Net.BCrypt;
using FoodDiary.Authentication.Contracts.Authentication.Common;

namespace FoodDiary.Modules.Identity.Infrastructure.Services;

public sealed class PasswordHasher : IPasswordHasher {
    private const string EnhancedHashPrefix = "$fd$bcrypt-sha384$";

    public string Hash(string password) =>
        EnhancedHashPrefix + BCryptHasher.EnhancedHashPassword(password, HashType.SHA384);

    [ExcludeFromCodeCoverage]
    public bool Verify(string password, string hashedPassword) {
        if (string.IsNullOrEmpty(password) || string.IsNullOrWhiteSpace(hashedPassword)) {
            return false;
        }

        try {
            return hashedPassword.StartsWith(EnhancedHashPrefix, StringComparison.Ordinal)
                ? BCryptHasher.EnhancedVerify(
                    password,
                    hashedPassword[EnhancedHashPrefix.Length..],
                    HashType.SHA384)
                : BCryptHasher.Verify(password, hashedPassword);
        } catch (SaltParseException) {
            return false;
        } catch (ArgumentException) {
            return false;
        }
    }

    public bool NeedsRehash(string hashedPassword) =>
        !hashedPassword.StartsWith(EnhancedHashPrefix, StringComparison.Ordinal);
}
