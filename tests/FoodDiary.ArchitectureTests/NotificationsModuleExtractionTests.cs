namespace FoodDiary.ArchitectureTests;

[ExcludeFromCodeCoverage]
public sealed class NotificationsModuleExtractionTests {
    [Theory]
    [InlineData("FoodDiary.Application.Notifications")]
    [InlineData("FoodDiary.Application.Abstractions/Notifications")]
    [InlineData("FoodDiary.Domain/Entities/Notifications")]
    [InlineData("FoodDiary.Infrastructure/Persistence/Notifications")]
    [InlineData("FoodDiary.Infrastructure/Persistence/Configurations/Notifications")]
    public void DonorDirectories_DoNotRetainNotificationOwnedCode(string relativePath) {
        string path = ArchitectureTestPaths.FromRoot(relativePath.Split('/'));
        Assert.Empty(Directory.Exists(path) ? SourceScanner.SourceFiles(path) : []);
    }

    [Fact]
    public void SharedContext_ExplicitlyRegistersModulePersistenceModel() {
        string source = File.ReadAllText(ArchitectureTestPaths.FromRoot("FoodDiary.Infrastructure", "Persistence", "FoodDiaryDbContext.cs"));
        Assert.Contains("ApplyNotificationsPersistenceModel()", source, StringComparison.Ordinal);
        Assert.True(File.Exists(ArchitectureTestPaths.FromRoot("Modules", "Notifications", "Infrastructure", "Model", "NotificationWebPushOutboxMessage.cs")));
        Assert.True(File.Exists(ArchitectureTestPaths.FromRoot("Modules", "Notifications", "Infrastructure", "Services", "WebPushNotificationSender.cs")));
    }

    [Fact]
    public void NotificationsApplicationSource_LivesOnlyInExtractedAssembly() {
        string legacyRoot = ArchitectureTestPaths.FromRoot("FoodDiary.Application", "Notifications");
        string extractedRoot = ArchitectureTestPaths.FromRoot("Modules", "Notifications", "Application");

        Assert.Empty(Directory.Exists(legacyRoot) ? SourceScanner.SourceFiles(legacyRoot) : []);
        Assert.NotEmpty(SourceScanner.SourceFiles(extractedRoot));
        Assert.True(File.Exists(Path.Combine(extractedRoot, "FoodDiary.Application.Notifications.csproj")));
    }

    [Fact]
    public void CoreApplication_DoesNotReferenceExtractedNotificationsAssembly() {
        string[] references = ProjectReferenceReader.ReadProjectReferences(
            "FoodDiary.Application.Runtime/FoodDiary.Application.Runtime.csproj");

        Assert.DoesNotContain("FoodDiary.Application.Notifications", references, StringComparer.Ordinal);
    }

    [Fact]
    public void ExtractedNotificationsAssembly_HasOnlyApprovedProjectReferences() {
        string[] references = ProjectReferenceReader.ReadProjectReferences(
            "Modules/Notifications/Application/FoodDiary.Application.Notifications.csproj");
        string[] expectedReferences = ["FoodDiary.Application.Contracts", "FoodDiary.Audit.Contracts", "FoodDiary.Mediator", "FoodDiary.Modules.Notifications.Application.Abstractions", "FoodDiary.Modules.Notifications.Domain", "FoodDiary.Modules.Users.Contracts", "FoodDiary.Modules.Users.Domain"];

        Assert.Equal(expectedReferences, references);
    }

    [Theory]
    [InlineData("FoodDiary.Web.Api/Extensions/ApiServiceCollectionExtensions.cs")]
    [InlineData("FoodDiary.JobManager/Program.cs")]
    [InlineData("FoodDiary.Initializer/Program.cs")]
    public void ExecutableCompositionRoots_RegisterNotificationsModule(string relativePath) {
        string source = File.ReadAllText(ArchitectureTestPaths.FromRoot(relativePath.Split('/')));

        Assert.Contains("AddNotificationsModule()", source, StringComparison.Ordinal);
    }
}
