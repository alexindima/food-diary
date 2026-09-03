namespace FoodDiary.ArchitectureTests;

[ExcludeFromCodeCoverage]
public sealed class IdentityPersistenceOwnershipTests {
    [Theory]
    [InlineData("FoodDiary.Infrastructure/Persistence/Users/UserLoginEventRepository.cs", "Modules/Identity/Infrastructure/Persistence/Users/UserLoginEventRepository.cs")]
    [InlineData("FoodDiary.Infrastructure/Persistence/Email/EmailTemplateProvider.cs", "Modules/Identity/Infrastructure/Persistence/Email/EmailTemplateProvider.cs")]
    [InlineData("tests/FoodDiary.Infrastructure.Tests/Services/EmailTemplateProviderTests.cs", "Modules/Identity/tests/FoodDiary.Modules.Identity.Infrastructure.Tests/EmailTemplateProviderTests.cs")]
    [InlineData("tests/FoodDiary.Infrastructure.IntegrationTests/Integration/UserLoginEventRepositoryIntegrationTests.cs", "Modules/Identity/tests/FoodDiary.Modules.Identity.Infrastructure.IntegrationTests/Integration/UserLoginEventRepositoryIntegrationTests.cs")]
    [InlineData("FoodDiary.Infrastructure/Persistence/Authentication/TelegramAssertionReplayGuard.cs", "Modules/Identity/Infrastructure/Persistence/Authentication/TelegramAssertionReplayGuard.cs")]
    [InlineData("FoodDiary.Infrastructure/Persistence/Authentication/ConsumedTelegramAssertion.cs", "Modules/Identity/Infrastructure/Model/Authentication/ConsumedTelegramAssertion.cs")]
    [InlineData("FoodDiary.Infrastructure/Persistence/Configurations/Authentication/ConsumedTelegramAssertionConfiguration.cs", "Modules/Identity/Infrastructure/Model/Configurations/Authentication/ConsumedTelegramAssertionConfiguration.cs")]
    public void IdentityPersistenceAdaptersAndFocusedTests_StayWithTheirOwner(string donorPath, string ownedPath) {
        Assert.False(File.Exists(ArchitectureTestPaths.FromRoot(donorPath)), $"Obsolete donor source: {donorPath}");
        Assert.True(File.Exists(ArchitectureTestPaths.FromRoot(ownedPath)), $"Missing Identity source: {ownedPath}");
    }

    [Fact]
    public void TelegramReplayProviderTests_StayWithIdentityInsteadOfMixedDietologistCoverage() {
        Assert.True(File.Exists(ArchitectureTestPaths.FromRoot("Modules/Identity/tests/FoodDiary.Modules.Identity.Infrastructure.IntegrationTests/Integration/TelegramAssertionReplayGuardIntegrationTests.cs")));
        string donor = File.ReadAllText(ArchitectureTestPaths.FromRoot("tests/FoodDiary.Infrastructure.IntegrationTests/Integration/DietologistPersistenceIntegrationTests.cs"));
        Assert.DoesNotContain("TelegramAssertionReplayGuard", donor, StringComparison.Ordinal);
    }
}
