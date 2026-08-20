using FoodDiary.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FoodDiary.Infrastructure.Tests.Persistence;

[ExcludeFromCodeCoverage]
public sealed class FoodDiaryDbContextFactoryTests {
    [Fact]
    public void CreateDbContext_WhenEnvironmentConnectionStringExists_UsesEnvironmentConnectionString() {
        const string variableName = "FOODDIARY_CONNECTION_STRING";
        string? previousValue = Environment.GetEnvironmentVariable(variableName);
        string? previousAppData = Environment.GetEnvironmentVariable("APPDATA");
        const string connectionString = "Host=localhost;Database=factory_test;Username=test;Password=test";
        Environment.SetEnvironmentVariable(variableName, connectionString);
        Environment.SetEnvironmentVariable("APPDATA", Path.Combine(Path.GetTempPath(), $"fd-user-secrets-{Guid.NewGuid():N}"));

        try {
            using FoodDiaryDbContext context = new FoodDiaryDbContextFactory().CreateDbContext([]);

            Assert.Equal(connectionString, context.Database.GetConnectionString());
        } finally {
            Environment.SetEnvironmentVariable(variableName, previousValue);
            Environment.SetEnvironmentVariable("APPDATA", previousAppData);
        }
    }

    [Fact]
    public void CreateDbContext_WithoutConfiguredConnectionString_DoesNotEmbedDefaultPassword() {
        const string variableName = "FOODDIARY_CONNECTION_STRING";
        string? previousValue = Environment.GetEnvironmentVariable(variableName);
        string? previousAppData = Environment.GetEnvironmentVariable("APPDATA");
        Environment.SetEnvironmentVariable(variableName, value: null);
        Environment.SetEnvironmentVariable("APPDATA", Path.Combine(Path.GetTempPath(), $"fd-user-secrets-{Guid.NewGuid():N}"));

        try {
            using FoodDiaryDbContext context = new FoodDiaryDbContextFactory().CreateDbContext([]);

            Assert.DoesNotContain(
                expectedSubstring: "Password=",
                actualString: context.Database.GetConnectionString(),
                comparisonType: StringComparison.OrdinalIgnoreCase);
        } finally {
            Environment.SetEnvironmentVariable(variableName, previousValue);
            Environment.SetEnvironmentVariable("APPDATA", previousAppData);
        }
    }
}
