using FoodDiary.Initializer;
using FoodDiary.Application.Authentication.Common;
using FoodDiary.Application.Abstractions.Notifications.Common;

namespace FoodDiary.Infrastructure.Tests.Services;

public sealed class InitializerTests {
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
    public void InitializerCommandParse_WithMissingConnectionStringValue_Throws() {
        var ex = Assert.Throws<InvalidOperationException>(() => InitializerCommand.Parse([
            "update",
            "--connection-string",
        ]));

        Assert.Contains("Missing value", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void InitializerCommandParse_WithUnexpectedArgument_Throws() {
        var ex = Assert.Throws<InvalidOperationException>(() => InitializerCommand.Parse([
            "rollback",
            "migration-a",
            "extra",
        ]));

        Assert.Contains("Unexpected argument", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task UsdaDataSeederSeedAsync_WhenDirectoryMissing_ThrowsBeforeUsingDbContext() {
        var missingDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));

        var ex = await Assert.ThrowsAsync<DirectoryNotFoundException>(() =>
            UsdaDataSeeder.SeedAsync(dbContext: null!, missingDirectory));

        Assert.Contains(missingDirectory, ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task UsdaDataSeederForceSeedAsync_WhenDirectoryMissing_ThrowsBeforeUsingDbContext() {
        var missingDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));

        var ex = await Assert.ThrowsAsync<DirectoryNotFoundException>(() =>
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
        var fields = UsdaCsvReader.ParseLine(line);

        Assert.Equal(expectedFields, fields);
    }

    [Fact]
    public void UsdaCsvReaderTruncate_WhenValueExceedsMaxLength_ReturnsPrefix() {
        var value = UsdaCsvReader.Truncate("abcdef", 3);

        Assert.Equal("abc", value);
    }

    [Fact]
    public void UsdaCsvReaderTruncate_WhenValueFits_ReturnsOriginalValue() {
        var value = UsdaCsvReader.Truncate("abc", 3);

        Assert.Equal("abc", value);
    }

    [Fact]
    public async Task UsdaCsvReaderReadLinesAsync_SkipsHeaderAndBlankLines() {
        var path = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.csv");
        await File.WriteAllTextAsync(path, "header\nfirst\n \nsecond\n", CancellationToken.None);
        try {
            var lines = new List<string>();
            await foreach (var line in UsdaCsvReader.ReadLinesAsync(path)) {
                lines.Add(line);
            }

            Assert.Equal(["first", "second"], lines);
        } finally {
            File.Delete(path);
        }
    }
}
