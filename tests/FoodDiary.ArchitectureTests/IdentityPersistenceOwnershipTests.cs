namespace FoodDiary.ArchitectureTests;

[ExcludeFromCodeCoverage]
public sealed class IdentityPersistenceOwnershipTests {
    [Theory]
    [InlineData("FoodDiary.Infrastructure/Persistence/Users/UserLoginEventRepository.cs", "Modules/Identity/Infrastructure/Persistence/Users/UserLoginEventRepository.cs")]
    [InlineData("FoodDiary.Infrastructure/Persistence/Email/EmailTemplateProvider.cs", "Modules/Identity/Infrastructure/Persistence/Email/EmailTemplateProvider.cs")]
    [InlineData("tests/FoodDiary.Infrastructure.Tests/Services/EmailTemplateProviderTests.cs", "Modules/Identity/tests/FoodDiary.Modules.Identity.Infrastructure.Tests/EmailTemplateProviderTests.cs")]
    [InlineData("tests/FoodDiary.Infrastructure.IntegrationTests/Integration/UserLoginEventRepositoryIntegrationTests.cs", "Modules/Identity/tests/FoodDiary.Modules.Identity.Infrastructure.IntegrationTests/Integration/UserLoginEventRepositoryIntegrationTests.cs")]
    public void IdentityPersistenceAdaptersAndFocusedTests_StayWithTheirOwner(string donorPath, string ownedPath) {
        Assert.False(File.Exists(ArchitectureTestPaths.FromRoot(donorPath)), $"Obsolete donor source: {donorPath}");
        Assert.True(File.Exists(ArchitectureTestPaths.FromRoot(ownedPath)), $"Missing Identity source: {ownedPath}");
    }
}
