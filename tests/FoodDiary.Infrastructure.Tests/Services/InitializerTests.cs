using FoodDiary.Initializer;
using FoodDiary.Application.Abstractions.Authentication.Common;
using FoodDiary.Application.Identity.Authentication.Commands.BootstrapInitialAdmin;
using FoodDiary.Application.Abstractions.Notifications.Common;
using FoodDiary.Results;
using Microsoft.Extensions.Configuration;

namespace FoodDiary.Infrastructure.Tests.Services;

[ExcludeFromCodeCoverage]
public sealed class InitializerTests {
    [Fact]
    public void InitialAdminBootstrapOptions_FromConfiguration_UsesDefaults() {
        IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection().Build();

        var options = InitialAdminBootstrapOptions.FromConfiguration(configuration);

        Assert.Multiple(
            () => Assert.Equal("admin@fooddiary.club", options.Email),
            () => Assert.Equal(string.Empty, options.Password),
            () => Assert.Equal(TimeSpan.FromSeconds(30), options.Timeout));
    }

    [Theory]
    [InlineData("0")]
    [InlineData("301")]
    public void InitialAdminBootstrapOptions_FromConfiguration_RejectsInvalidTimeout(string timeout) {
        IConfiguration configuration = CreateInitialAdminConfiguration(
            email: "admin@example.com",
            password: "",
            timeout);

        Assert.Throws<InvalidOperationException>(() =>
            InitialAdminBootstrapOptions.FromConfiguration(configuration));
    }

    [Theory]
    [InlineData("admin@example.com", "short")]
    [InlineData("admin@example.com", "123456")]
    [InlineData("not-an-email", "long-enough-password")]
    [InlineData("", "long-enough-password")]
    public void InitialAdminBootstrapOptions_FromConfiguration_RejectsInvalidCredentials(
        string email,
        string password) {
        IConfiguration configuration = CreateInitialAdminConfiguration(email, password, "30");

        Assert.Throws<InvalidOperationException>(() =>
            InitialAdminBootstrapOptions.FromConfiguration(configuration));
    }

    [Fact]
    public void InitialAdminBootstrapOptions_FromConfiguration_AcceptsConfiguredValues() {
        IConfiguration configuration = CreateInitialAdminConfiguration(
            "admin@example.com",
            "long-enough-password",
            "45");

        var options = InitialAdminBootstrapOptions.FromConfiguration(configuration);

        Assert.Multiple(
            () => Assert.Equal("admin@example.com", options.Email),
            () => Assert.Equal("long-enough-password", options.Password),
            () => Assert.Equal(TimeSpan.FromSeconds(45), options.Timeout));
    }

    [Theory]
    [InlineData(BootstrapInitialAdminStatus.SkippedMissingPassword)]
    [InlineData(BootstrapInitialAdminStatus.SkippedExistingUser)]
    [InlineData(BootstrapInitialAdminStatus.Created)]
    public async Task InitialAdminBootstrapper_BootstrapAsync_AcceptsKnownOutcomes(
        BootstrapInitialAdminStatus status) {
        IInitialAdminBootstrapService service = Substitute.For<IInitialAdminBootstrapService>();
        service.BootstrapAsync(Arg.Any<string>(), "password", Arg.Any<CancellationToken>())
            .Returns(Result.Success(new BootstrapInitialAdminModel(status, "admin@example.com")));

        await InitialAdminBootstrapper.BootstrapAsync(
            service,
            new InitialAdminBootstrapOptions(" admin@example.com ", "password", TimeSpan.FromSeconds(1)));

        await service.Received(1).BootstrapAsync(
            " admin@example.com ",
            "password",
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task InitialAdminBootstrapper_BootstrapAsync_WhenServiceFails_Throws() {
        IInitialAdminBootstrapService service = Substitute.For<IInitialAdminBootstrapService>();
        service.BootstrapAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure<BootstrapInitialAdminModel>(
                new Error("InitialAdmin.Failed", "failed")));

        InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            InitialAdminBootstrapper.BootstrapAsync(
                service,
                new InitialAdminBootstrapOptions("admin@example.com", "password", TimeSpan.FromSeconds(1))));

        Assert.Contains("InitialAdmin.Failed", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task InitialAdminBootstrapper_BootstrapAsync_WhenInternalTimeoutExpires_ThrowsTimeout() {
        IInitialAdminBootstrapService service = Substitute.For<IInitialAdminBootstrapService>();
        service.BootstrapAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(async call => {
                await Task.Delay(
                    Timeout.InfiniteTimeSpan,
                    call.ArgAt<CancellationToken>(2)).ConfigureAwait(false);
                return Result.Success(new BootstrapInitialAdminModel(
                    BootstrapInitialAdminStatus.Created,
                    "admin@example.com"));
            });

        await Assert.ThrowsAsync<TimeoutException>(() =>
            InitialAdminBootstrapper.BootstrapAsync(
                service,
                new InitialAdminBootstrapOptions(
                    "admin@example.com",
                    "password",
                    TimeSpan.FromMilliseconds(10))));
    }

    [Fact]
    public async Task InitialAdminBootstrapper_BootstrapAsync_WhenCallerCancels_PropagatesCancellation() {
        using var cancellationSource = new CancellationTokenSource();
        await cancellationSource.CancelAsync();
        IInitialAdminBootstrapService service = Substitute.For<IInitialAdminBootstrapService>();
        service.BootstrapAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(call => Task.FromCanceled<Result<BootstrapInitialAdminModel>>(
                call.ArgAt<CancellationToken>(2)));

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            InitialAdminBootstrapper.BootstrapAsync(
                service,
                new InitialAdminBootstrapOptions(
                    "admin@example.com",
                    "password",
                    TimeSpan.FromSeconds(1)),
                cancellationSource.Token));
    }

    [Fact]
    public async Task InitialAdminBootstrapper_BootstrapAsync_WhenOutcomeIsUnknown_Throws() {
        IInitialAdminBootstrapService service = Substitute.For<IInitialAdminBootstrapService>();
        service.BootstrapAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Result.Success(new BootstrapInitialAdminModel(
                (BootstrapInitialAdminStatus)999,
                "admin@example.com")));

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            InitialAdminBootstrapper.BootstrapAsync(
                service,
                new InitialAdminBootstrapOptions("admin@example.com", "password", TimeSpan.FromSeconds(1))));
    }

    [Fact]
    public void InitializerCommandParse_WithNoArguments_ReturnsNull() {
        var command = InitializerCommand.Parse([]);

        Assert.Null(command);
    }

    [Fact]
    public void InitializerCommandParse_WithTargetAndShortOptions_ParsesCommand() {
        var command = InitializerCommand.Parse([
            "rollback",
            "0",
            "-c",
            "Host=localhost;Database=fooddiary",
            "-f",
        ]);

        Assert.NotNull(command);
        Assert.Equal("rollback", command.Name);
        Assert.Equal("0", command.TargetMigration);
        Assert.Equal("Host=localhost;Database=fooddiary", command.ConnectionString);
        Assert.True(command.Force);
    }

    [Fact]
    public void InitializerCommandParse_WithConnectionStringAndForce_ParsesCommand() {
        var command = InitializerCommand.Parse([
            "seed-usda",
            "C:/data/usda",
            "--connection-string",
            "Host=localhost;Database=fooddiary",
            "--force",
        ]);

        Assert.NotNull(command);
        Assert.Equal("seed-usda", command.Name);
        Assert.Equal("C:/data/usda", command.TargetMigration);
        Assert.Equal("Host=localhost;Database=fooddiary", command.ConnectionString);
        Assert.True(command.Force);
    }

    [Fact]
    public void InitializerCommandParse_WithSafeReplayOptions_ParsesCommand() {
        var command = InitializerCommand.Parse([
            "replay-outbox",
            "email:98d9e58e-f9cd-4d7d-84dd-4c81ef48bc7c",
            "--dry-run",
            "--limit",
            "25",
            "--expected-attempt-count",
            "3",
        ]);

        Assert.NotNull(command);
        Assert.True(command.DryRun);
        Assert.Equal(25, command.Limit);
        Assert.Equal(3, command.ExpectedAttemptCount);
    }

    [Fact]
    public void InitializerCommandParse_WithAuditOptions_ParsesCommand() {
        var command = InitializerCommand.Parse([
            "replay-outbox",
            "--requested-by",
            "operator@example.test",
            "--reason",
            "incident recovery",
        ]);

        Assert.NotNull(command);
        Assert.Multiple(
            () => Assert.Equal("operator@example.test", command.RequestedBy),
            () => Assert.Equal("incident recovery", command.Reason));
    }

    [Theory]
    [InlineData("--requested-by")]
    [InlineData("--reason")]
    public void InitializerCommandParse_WithMissingAuditOptionValue_Throws(string option) {
        InvalidOperationException ex = Assert.Throws<InvalidOperationException>(() =>
            InitializerCommand.Parse(["replay-outbox", option]));

        Assert.Contains($"Missing value for {option}", ex.Message, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("0")]
    [InlineData("-1")]
    [InlineData("many")]
    public void InitializerCommandParse_WithInvalidLimit_Throws(string limit) {
        InvalidOperationException ex = Assert.Throws<InvalidOperationException>(() => InitializerCommand.Parse([
            "list-dead-letters",
            "--limit",
            limit,
        ]));

        Assert.Contains("positive integer", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void InitializerCommandParse_WithMissingConnectionStringValue_Throws() {
        InvalidOperationException ex = Assert.Throws<InvalidOperationException>(() => InitializerCommand.Parse([
            "update",
            "--connection-string",
        ]));

        Assert.Contains("Missing value", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void InitializerCommandParse_WithUnexpectedArgument_Throws() {
        InvalidOperationException ex = Assert.Throws<InvalidOperationException>(() => InitializerCommand.Parse([
            "rollback",
            "migration-a",
            "extra",
        ]));

        Assert.Contains("Unexpected argument", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task UsdaDataSeederSeedAsync_WhenDirectoryMissing_ThrowsBeforeUsingDbContext() {
        string missingDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));

        DirectoryNotFoundException ex = await Assert.ThrowsAsync<DirectoryNotFoundException>(() =>
            UsdaDataSeeder.SeedAsync(dbContext: null!, missingDirectory));

        Assert.Contains(missingDirectory, ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task UsdaDataSeederForceSeedAsync_WhenDirectoryMissing_ThrowsBeforeUsingDbContext() {
        string missingDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));

        DirectoryNotFoundException ex = await Assert.ThrowsAsync<DirectoryNotFoundException>(() =>
            UsdaDataSeeder.ForceSeedAsync(dbContext: null!, missingDirectory));

        Assert.Contains(missingDirectory, ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task NoOpEmailVerificationNotifier_Completes() {
        IEmailVerificationNotifier notifier = new NoOpEmailVerificationNotifier();

        await notifier.NotifyEmailVerifiedAsync(Guid.NewGuid(), CancellationToken.None);
    }

    [Fact]
    public async Task NoOpNotificationPusher_Completes() {
        INotificationPusher pusher = new NoOpNotificationPusher();

        await pusher.PushUnreadCountAsync(Guid.NewGuid(), 3, CancellationToken.None);
        await pusher.PushNotificationsChangedAsync(Guid.NewGuid(), CancellationToken.None);
    }

    [Theory]
    [InlineData("1,sr_legacy_food,\"Apple, raw\",10", new[] { "1", "sr_legacy_food", "Apple, raw", "10" })]
    [InlineData(" 1 , \"quoted \"\"value\"\"\" ,mg ", new[] { "1", "quoted \"value\"", "mg" })]
    [InlineData("1,,3", new[] { "1", "", "3" })]
    public void UsdaCsvReaderParseLine_ReturnsFields(string line, string[] expectedFields) {
        string[] fields = UsdaCsvReader.ParseLine(line);

        Assert.Equal(expectedFields, fields);
    }

    [Fact]
    public void UsdaCsvReaderTruncate_WhenValueExceedsMaxLength_ReturnsPrefix() {
        string value = UsdaCsvReader.Truncate("abcdef", 3);

        Assert.Equal("abc", value);
    }

    [Fact]
    public void UsdaCsvReaderTruncate_WhenValueFits_ReturnsOriginalValue() {
        string value = UsdaCsvReader.Truncate("abc", 3);

        Assert.Equal("abc", value);
    }

    [Fact]
    public async Task UsdaCsvReaderReadLinesAsync_SkipsHeaderAndBlankLines() {
        string path = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.csv");
        await File.WriteAllTextAsync(path, "header\nfirst\n \nsecond\n", CancellationToken.None);
        try {
            var lines = new List<string>();
            await foreach (string line in UsdaCsvReader.ReadLinesAsync(path)) {
                lines.Add(line);
            }

            Assert.Equal(["first", "second"], lines);
        } finally {
            File.Delete(path);
        }
    }

    private static IConfiguration CreateInitialAdminConfiguration(
        string email,
        string password,
        string timeout) =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>(StringComparer.Ordinal) {
                ["InitialAdmin:Email"] = email,
                ["InitialAdmin:Password"] = password,
                ["InitialAdmin:BootstrapTimeoutSeconds"] = timeout,
            })
            .Build();
}
