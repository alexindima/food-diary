using Npgsql;

namespace FoodDiary.MailInbox.Infrastructure.Options;

public static class MailInboxDatabaseConfiguration {
    public const string RequiredDatabaseName = "fooddiary_mailinbox";
    public const string RuntimeRoleConfigurationKey = "MailInboxRuntimeDatabase:RoleName";

    public static bool TargetsRequiredDatabase(string? connectionString) {
        if (string.IsNullOrWhiteSpace(connectionString)) {
            return false;
        }

        try {
            var builder = new NpgsqlConnectionStringBuilder(connectionString);
            return string.Equals(
                builder.Database,
                RequiredDatabaseName,
                StringComparison.OrdinalIgnoreCase);
        } catch (ArgumentException) {
            return false;
        }
    }

    public static bool UsesAuthenticatedTls(string? connectionString) {
        if (string.IsNullOrWhiteSpace(connectionString)) {
            return false;
        }

        try {
            var builder = new NpgsqlConnectionStringBuilder(connectionString);
            return builder.SslMode == SslMode.VerifyFull;
        } catch (ArgumentException) {
            return false;
        }
    }

    public static bool UsesExpectedRole(string? connectionString, string? expectedRoleName) {
        if (string.IsNullOrWhiteSpace(connectionString) || string.IsNullOrWhiteSpace(expectedRoleName)) {
            return false;
        }

        try {
            var builder = new NpgsqlConnectionStringBuilder(connectionString);
            return string.Equals(
                builder.Username,
                expectedRoleName,
                StringComparison.Ordinal);
        } catch (ArgumentException) {
            return false;
        }
    }
}
