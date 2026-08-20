using System.Data.Common;

namespace FoodDiary.MailInbox.WebApi;

internal static class MailInboxHostConfiguration {
    internal const string LocalDatabasePasswordPlaceholder = "change-me-local-password";
    private static readonly string[] DatabasePasswordKeys = ["Password", "Pwd"];
    private const string ValidationFailureMessage =
        "Production requires a valid ConnectionStrings:DefaultConnection that does not use the repository-local database password.";

    internal static IServiceCollection AddMailInboxHostConfigurationValidation(this IServiceCollection services) {
        services.AddOptions<MailInboxHostConfigurationOptions>()
            .Configure<IConfiguration, IHostEnvironment>((options, configuration, environment) => {
                options.IsProduction = environment.IsProduction();
                options.ConnectionString = configuration.GetConnectionString("DefaultConnection");
            })
            .Validate(static options => GetValidationFailure(options.ConnectionString, options.IsProduction) is null,
                ValidationFailureMessage)
            .ValidateOnStart();

        return services;
    }

    internal static void Validate(IConfiguration configuration, IHostEnvironment environment) {
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(environment);

        string? failure = GetValidationFailure(
            configuration.GetConnectionString("DefaultConnection"),
            environment.IsProduction());
        if (failure is not null) {
            throw new InvalidOperationException(failure);
        }
    }

    private static string? GetValidationFailure(string? connectionString, bool isProduction) {
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
    }
}
