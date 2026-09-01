namespace FoodDiary.ArchitectureTests;

[ExcludeFromCodeCoverage]
public sealed class AdminModuleExtractionTests {
    [Theory]
    [InlineData("Application", "FoodDiary.Modules.Admin.Application.csproj")]
    [InlineData("Application/Abstractions", "FoodDiary.Modules.Admin.Application.Abstractions.csproj")]
    [InlineData("Domain", "FoodDiary.Modules.Admin.Domain.csproj")]
    [InlineData("Infrastructure", "FoodDiary.Modules.Admin.Infrastructure.csproj")]
    [InlineData("Infrastructure/Model", "FoodDiary.Modules.Admin.PersistenceModel.csproj")]
    public void OwnedLayers_HavePhysicalProjects(string folder, string project) {
        Assert.True(File.Exists(ArchitectureTestPaths.FromRoot("Modules", "Admin", folder, project)));
    }

    [Fact]
    public void ImpersonationDomainAndModel_PreserveOneWayOwnership() {
        Assert.Equal(["FoodDiary.Domain"], ProjectReferenceReader.ReadProjectReferences(
            "Modules/Admin/Domain/FoodDiary.Modules.Admin.Domain.csproj"));
        Assert.DoesNotContain("FoodDiary.Modules.Admin.Domain", ProjectReferenceReader.ReadProjectReferences(
            "FoodDiary.Domain/FoodDiary.Domain.csproj"), StringComparer.Ordinal);
        Assert.False(File.Exists(ArchitectureTestPaths.FromRoot("FoodDiary.Domain/Entities/Admin/AdminImpersonationSession.cs")));
        Assert.True(File.Exists(ArchitectureTestPaths.FromRoot("Modules/Admin/Domain/Entities/Admin/AdminImpersonationSession.cs")));
        string[] references = ProjectReferenceReader.ReadProjectReferences("FoodDiary.Infrastructure/FoodDiary.Infrastructure.csproj");
        Assert.Contains("FoodDiary.Modules.Admin.PersistenceModel", references, StringComparer.Ordinal);
        Assert.DoesNotContain("FoodDiary.Modules.Admin.Infrastructure", references, StringComparer.Ordinal);
        Assert.Contains("ApplyAdminPersistenceModel()", File.ReadAllText(
            ArchitectureTestPaths.FromRoot("FoodDiary.Infrastructure/Persistence/FoodDiaryDbContext.cs")), StringComparison.Ordinal);
    }

    [Fact]
    public void ForeignEmailAndUserAudit_StayWithExistingOwners() {
        Assert.True(File.Exists(ArchitectureTestPaths.FromRoot("FoodDiary.Infrastructure/Persistence/Admin/EmailTemplateRepository.cs")));
        Assert.True(File.Exists(ArchitectureTestPaths.FromRoot("FoodDiary.Infrastructure/Persistence/Admin/AdminUserRoleAuditRepository.cs")));
        Assert.True(File.Exists(ArchitectureTestPaths.FromRoot("FoodDiary.Application.Identity/Email/Services/EmailTemplateAdministrationService.cs")));
        Assert.True(File.Exists(ArchitectureTestPaths.FromRoot("Modules/Users/Application/Services/UserAdministrationMutationService.cs")));
        Assert.Empty(SourceScanner.FindLinePatternViolations(ArchitectureTestPaths.FromRoot("Modules/Admin/Domain"),
            ["class EmailTemplate", "class UserRoleAuditEvent"]));
    }

    [Theory]
    [InlineData("Application", "Admin/AdminValidatorTests.cs")]
    [InlineData("Application", "Admin/AdminAchievementDefinitionHandlerTests.cs")]
    [InlineData("Application", "Admin/GetAdminUsersQueryHandlerTests.cs")]
    [InlineData("Application", "Admin/GetCollaborationAuditQueryHandlerTests.cs")]
    [InlineData("Application", "Admin/CreateAdminUserCommandValidatorTests.cs")]
    [InlineData("Domain", "Domain/AdminInvariantTests.cs")]
    public void FocusedTests_LiveOnlyInModuleProjects(string layer, string file) {
        Assert.True(File.Exists(ArchitectureTestPaths.FromRoot($"Modules/Admin/tests/FoodDiary.Modules.Admin.{layer}.Tests/{file}")));
        Assert.False(File.Exists(ArchitectureTestPaths.FromRoot($"tests/FoodDiary.{layer}.Tests/{file}")));
    }

    [Fact]
    public void LegacyApplicationAssemblyIdentity_IsPreserved() {
        string project = File.ReadAllText(ArchitectureTestPaths.FromRoot("Modules/Admin/Application/FoodDiary.Modules.Admin.Application.csproj"));
        Assert.Contains("<AssemblyName>FoodDiary.Application.Admin</AssemblyName>", project, StringComparison.Ordinal);
        string legacy = ArchitectureTestPaths.FromRoot("FoodDiary.Application.Admin");
        Assert.Empty(Directory.Exists(legacy) ? SourceScanner.SourceFiles(legacy) : []);
    }

    [Fact]
    public void AdminApplicationSource_LivesOnlyInExtractedAssembly() {
        string legacyRoot = ArchitectureTestPaths.FromRoot("FoodDiary.Application", "Admin");
        string extractedRoot = ArchitectureTestPaths.FromRoot("Modules", "Admin", "Application");
        Assert.Empty(Directory.Exists(legacyRoot) ? SourceScanner.SourceFiles(legacyRoot) : []);
        Assert.NotEmpty(SourceScanner.SourceFiles(extractedRoot));
    }

    [Fact]
    public void ExtractedAdminAssembly_HasOnlyApprovedProjectReferences() {
        string[] references = ProjectReferenceReader.ReadProjectReferences(
            "Modules/Admin/Application/FoodDiary.Modules.Admin.Application.csproj");
        Assert.Equal([
            "FoodDiary.Application.Abstractions",
            "FoodDiary.Domain",
            "FoodDiary.Mediator",
            "FoodDiary.Modules.Admin.Application.Abstractions",
            "FoodDiary.Modules.Ai.Application",
            "FoodDiary.Modules.ContentReports.Contracts",
            "FoodDiary.Modules.Gamification.Application",
            "FoodDiary.Modules.Lessons.Contracts",
        ], references);
    }

    [Theory]
    [InlineData("FoodDiary.Web.Api/Extensions/ApiServiceCollectionExtensions.cs")]
    [InlineData("FoodDiary.Initializer/Program.cs")]
    public void ExecutableCompositionRoots_RegisterAdminModule(string relativePath) {
        string source = File.ReadAllText(ArchitectureTestPaths.FromRoot(relativePath.Split('/')));
        Assert.Contains("AddAdminModule()", source, StringComparison.Ordinal);
    }
}
