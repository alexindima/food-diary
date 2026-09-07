using System.Data.Common;
using FoodDiary.MailInbox.Infrastructure.Options;
using FoodDiary.MailInbox.Infrastructure.Services;
using Microsoft.Extensions.Options;

namespace FoodDiary.MailInbox.WebApi;

internal static class MailInboxHostConfiguration {
    internal const string LocalDatabasePasswordPlaceholder = "change-me-local-password";
    private static readonly string[] DatabasePasswordKeys = ["Password", "Pwd"];
    private const string ValidationFailureMessage =
        "Production requires a provisioner-owned least-privilege database role, authenticated database TLS for the dedicated MailInbox database, does not use the repository-local database password, and configures a trusted SMTP relay network when SMTP is enabled.";

    internal static IServiceCollection AddMailInboxHostConfigurationValidation(this IServiceCollection services) {
        services.AddOptions<MailInboxHostConfigurationOptions>()
            .Configure<IConfiguration, IHostEnvironment>((options, configuration, environment) => {
                options.IsProduction = environment.IsProduction();
                options.ConnectionString = configuration.GetConnectionString("DefaultConnection");
                options.RuntimeRoleName = configuration[MailInboxDatabaseConfiguration.RuntimeRoleConfigurationKey];
                options.IsSmtpEnabled = configuration.GetValue("MailInboxSmtp:Enabled", defaultValue: true);
                options.TrustedRelayNetworks = configuration
                    .GetSection("MailInboxSmtp:TrustedRelayNetworks")
                    .Get<string[]>() ?? [];
            })
            .Validate(static options => GetValidationFailure(
                    options.ConnectionString,
                    options.RuntimeRoleName,
                    options.IsProduction,
                    options.IsSmtpEnabled,
                    options.TrustedRelayNetworks) is null,
                ValidationFailureMessage)
            .ValidateOnStart();

        return services;
    }

    internal static void Validate(IConfiguration configuration, IHostEnvironment environment) {
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(environment);

        string? failure = GetValidationFailure(
            configuration.GetConnectionString("DefaultConnection"),
            configuration[MailInboxDatabaseConfiguration.RuntimeRoleConfigurationKey],
            environment.IsProduction(),
            configuration.GetValue("MailInboxSmtp:Enabled", defaultValue: true),
            configuration.GetSection("MailInboxSmtp:TrustedRelayNetworks").Get<string[]>() ?? []);
        if (failure is not null) {
            throw new InvalidOperationException(failure);
        }
    }

    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
    internal static async Task ValidateRuntimeDatabaseRoleAsync(
        IServiceProvider services,
        IConfiguration configuration,
        IHostEnvironment environment,
        CancellationToken cancellationToken) {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(environment);
        if (!environment.IsProduction()) {
            return;
        }

        _ = services.GetRequiredService<IOptions<MailInboxHostConfigurationOptions>>().Value;
        string runtimeRoleName = configuration[MailInboxDatabaseConfiguration.RuntimeRoleConfigurationKey]
            ?? throw new InvalidOperationException(
                $"{MailInboxDatabaseConfiguration.RuntimeRoleConfigurationKey} must be configured in Production.");
        NpgsqlMailInboxRuntimeRoleValidator validator =
            services.GetRequiredService<NpgsqlMailInboxRuntimeRoleValidator>();
        await validator.ValidateAsync(runtimeRoleName, cancellationToken).ConfigureAwait(false);
    }

    private static string? GetValidationFailure(
        string? connectionString,
        string? runtimeRoleName,
        bool isProduction,
        bool isSmtpEnabled,
        IReadOnlyCollection<string> trustedRelayNetworks) {
        if (!isProduction) {
            return null;
        }

        if (string.IsNullOrWhiteSpace(connectionString)) {
            return "ConnectionStrings:DefaultConnection must be configured in Production.";
        }

        DbConnectionStringBuilder builder;
        try {
            builder = new DbConnectionStringBuilder {
                ConnectionString = connectionString,
            };
        } catch (ArgumentException) {
            return "ConnectionStrings:DefaultConnection must be a valid database connection string in Production.";
        }

        if (!HasNonEmptyDatabasePassword(builder)) {
            return "ConnectionStrings:DefaultConnection must include a non-empty database password in Production.";
        }

        if (ContainsLocalPasswordPlaceholder(builder)) {
            return "ConnectionStrings:DefaultConnection must not use the repository-local database password in Production.";
        }

        if (!MailInboxDatabaseConfiguration.TargetsRequiredDatabase(connectionString)) {
            return $"ConnectionStrings:DefaultConnection must target the dedicated '{MailInboxDatabaseConfiguration.RequiredDatabaseName}' database in Production.";
        }

        if (!NpgsqlMailInboxRuntimeRoleProvisioner.HasValidRoleName(runtimeRoleName)) {
            return $"{MailInboxDatabaseConfiguration.RuntimeRoleConfigurationKey} must identify a dedicated mailinbox_* role in Production.";
        }

        if (!MailInboxDatabaseConfiguration.UsesExpectedRole(connectionString, runtimeRoleName)) {
            return $"ConnectionStrings:DefaultConnection Username must match {MailInboxDatabaseConfiguration.RuntimeRoleConfigurationKey} in Production.";
        }

        if (!MailInboxDatabaseConfiguration.UsesAuthenticatedTls(connectionString)) {
            return "ConnectionStrings:DefaultConnection must use SSL Mode=VerifyFull in Production.";
        }

        if (isSmtpEnabled && trustedRelayNetworks.Count == 0) {
            return "MailInboxSmtp:TrustedRelayNetworks must contain at least one trusted upstream relay network in Production.";
        }

        return null;
    }

    private static bool HasNonEmptyDatabasePassword(DbConnectionStringBuilder builder) {
        bool found = false;
        foreach (string key in DatabasePasswordKeys) {
            if (!builder.TryGetValue(key, out object? value)) {
                continue;
            }

            found = true;
            if (string.IsNullOrWhiteSpace(
                    Convert.ToString(value, System.Globalization.CultureInfo.InvariantCulture))) {
                return false;
            }
        }

        return found;
    }

    private static bool ContainsLocalPasswordPlaceholder(DbConnectionStringBuilder builder) =>
        HasLocalPasswordPlaceholder(builder, "Password") ||
        HasLocalPasswordPlaceholder(builder, "Pwd");

    private static bool HasLocalPasswordPlaceholder(DbConnectionStringBuilder builder, string key) =>
        builder.TryGetValue(key, out object? value) &&
        string.Equals(
            Convert.ToString(value, System.Globalization.CultureInfo.InvariantCulture)!.Trim(),
            LocalDatabasePasswordPlaceholder,
            StringComparison.OrdinalIgnoreCase);

    private sealed class MailInboxHostConfigurationOptions {
        public bool IsProduction { get; set; }
        public string? ConnectionString { get; set; }
        public string? RuntimeRoleName { get; set; }
        public bool IsSmtpEnabled { get; set; }
        public string[] TrustedRelayNetworks { get; set; } = [];
    }
}
