using FoodDiary.Initializer;

namespace FoodDiary.Infrastructure.Tests.Services;

public sealed class InitializerTests {
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
}
