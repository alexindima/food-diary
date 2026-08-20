using FoodDiary.MailInbox.Application.Abstractions;
using FoodDiary.MailInbox.Infrastructure.Options;
using FoodDiary.MailInbox.Infrastructure.Services;
using FoodDiary.MailInbox.Initializer.Options;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using System.Reflection;

namespace FoodDiary.MailInbox.Initializer.Tests;

[ExcludeFromCodeCoverage]
public sealed class MailInboxInitializerCommandTests {
    [Fact]
    public void Parse_WhenArgsAreEmpty_ReturnsNull() {
        object? command = Parse();

        Assert.Null(command);
    }

    [Theory]
    [InlineData("status")]
    [InlineData("update")]
    public void Parse_WithKnownCommand_ReturnsCommand(string name) {
        object? command = Parse(name);

        Assert.NotNull(command);
        Assert.Equal(name, GetProperty<string>(command, "Name"));
    }

    [Theory]
    [InlineData("--connection-string")]
    [InlineData("-c")]
    [InlineData("--connection-string=Host=localhost;Database=mailinbox;Password=secret")]
    public void Parse_WhenConnectionStringOptionExists_Throws(string option) {
        TargetInvocationException exception = Assert.Throws<TargetInvocationException>(() =>
            Parse("update", option, "Host=localhost;Database=mailinbox;Password=secret"));

        InvalidOperationException innerException = Assert.IsType<InvalidOperationException>(exception.InnerException);
        Assert.Multiple(
            () => Assert.Equal("Unexpected argument.", innerException.Message),
            () => Assert.DoesNotContain("Password", innerException.Message, StringComparison.OrdinalIgnoreCase),
            () => Assert.DoesNotContain("secret", innerException.Message, StringComparison.Ordinal));
    }

    [Fact]
    public void Parse_WhenUnexpectedArgumentExists_Throws() {
        TargetInvocationException exception = Assert.Throws<TargetInvocationException>(() => Parse("status", "unexpected"));

        Assert.IsType<InvalidOperationException>(exception.InnerException);
    }

    [Fact]
    public void AddMailInboxInitializerServices_ResolvesSchemaInitializer() {
        var services = new ServiceCollection();
        IConfiguration configuration = new ConfigurationBuilder().Build();

        services.AddMailInboxInitializerServices(
            "Host=localhost;Database=fooddiary_mailinbox;Username=test;Password=test",
            configuration);
        using ServiceProvider provider = services.BuildServiceProvider();

        DmarcReportParser parser = provider.GetRequiredService<DmarcReportParser>();
        IMailInboxDmarcReportParser parserAbstraction = provider.GetRequiredService<IMailInboxDmarcReportParser>();
        IMailInboxSchemaInitializer schemaInitializer = provider.GetRequiredService<IMailInboxSchemaInitializer>();
        IMailInboxReadinessChecker readinessChecker = provider.GetRequiredService<IMailInboxReadinessChecker>();
        NpgsqlMailInboxRuntimeRoleProvisioner runtimeRoleProvisioner =
            provider.GetRequiredService<NpgsqlMailInboxRuntimeRoleProvisioner>();
        MailInboxStorageOptions storageOptions = provider.GetRequiredService<IOptions<MailInboxStorageOptions>>().Value;

        Assert.Multiple(
            () => Assert.Same(parser, parserAbstraction),
            () => Assert.NotNull(schemaInitializer),
            () => Assert.NotNull(readinessChecker),
            () => Assert.NotNull(runtimeRoleProvisioner),
            () => Assert.True(MailInboxStorageOptions.HasValidConfiguration(storageOptions)));
    }

    [Theory]
    [InlineData(false, "", "", true)]
    [InlineData(true, "mailinbox_runtime", "0123456789abcdef0123456789abcdef", true)]
    [InlineData(true, "pg_read_all_data", "0123456789abcdef0123456789abcdef", false)]
    [InlineData(true, "Invalid-Role", "0123456789abcdef0123456789abcdef", false)]
    [InlineData(true, "mailinbox_runtime", "too-short", false)]
    public void RuntimeDatabaseOptions_ValidateExpectedConfiguration(
        bool provisionRole,
        string roleName,
        string password,
        bool expected) {
        var options = new MailInboxRuntimeDatabaseOptions {
            ProvisionRole = provisionRole,
            RoleName = roleName,
            Password = password,
        };

        Assert.Equal(expected, MailInboxRuntimeDatabaseOptions.HasValidConfiguration(options));
    }

    private static object? Parse(params string[] args) {
        Type? type = Assembly.Load("FoodDiary.MailInbox.Initializer")
            .GetType("FoodDiary.MailInbox.Initializer.InitializerCommand");
        MethodInfo? method = type!.GetMethod("Parse", BindingFlags.Public | BindingFlags.Static);
        return method!.Invoke(null, [args]);
    }

    private static TValue? GetProperty<TValue>(object instance, string name) =>
        (TValue?)instance.GetType().GetProperty(name)!.GetValue(instance);
}
