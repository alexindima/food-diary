using FoodDiary.MailInbox.Infrastructure.Services;

namespace FoodDiary.MailInbox.Initializer.Options;

internal sealed class MailInboxRuntimeDatabaseOptions {
    public const string SectionName = "MailInboxRuntimeDatabase";

    public bool ProvisionRole { get; init; }

    public string RoleName { get; init; } = string.Empty;

    public string Password { get; init; } = string.Empty;

    public static bool HasValidConfiguration(MailInboxRuntimeDatabaseOptions options) =>
        !options.ProvisionRole ||
        (NpgsqlMailInboxRuntimeRoleProvisioner.HasValidRoleName(options.RoleName) &&
         NpgsqlMailInboxRuntimeRoleProvisioner.HasValidPassword(options.Password));
}
