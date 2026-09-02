namespace FoodDiary.ArchitectureTests;

[ExcludeFromCodeCoverage]
public sealed class UsersModuleExtractionTests {
    [Fact]
    public void CentralDomain_HasNoEntityDefinitionsOrModuleReferences() {
        string root = ArchitectureTestPaths.FromRoot("FoodDiary.Domain", "Entities");
        Assert.Empty(Directory.Exists(root) ? SourceScanner.SourceFiles(root) : []);
        Assert.Equal(["FoodDiary.Domain.Primitives"], ProjectReferenceReader.ReadProjectReferences(
            "FoodDiary.Domain/FoodDiary.Domain.csproj"));
    }

    [Fact]
    public void UsersContracts_HaveOnlyApprovedTypesAndSharedPrimitiveDependency() {
        Assert.Equal(["FoodDiary.Domain.Primitives"], ProjectReferenceReader.ReadProjectReferences(
            "Modules/Users/Domain.Contracts/FoodDiary.Modules.Users.Domain.Contracts.csproj"));
        string root = ArchitectureTestPaths.FromRoot("Modules", "Users", "Domain.Contracts");
        Assert.Equal(["Enums/ActivityLevel.cs", "ValueObjects/Ids/UserId.cs"],
            SourceScanner.SourceFiles(root).Select(path => Path.GetRelativePath(root, path).Replace('\\', '/')), StringComparer.Ordinal);
    }

    [Fact]
    public void ActivityLevel_IsOwnedOnlyByUsersDomainContracts() {
        Type enumType = typeof(FoodDiary.Domain.Enums.ActivityLevel);
        Assert.Equal("FoodDiary.Modules.Users.Domain.Contracts", enumType.Assembly.GetName().Name);
        Assert.Equal("FoodDiary.Domain.Enums", enumType.Namespace);
        Assert.True(File.Exists(ArchitectureTestPaths.FromRoot("Modules", "Users", "Domain.Contracts", "Enums", "ActivityLevel.cs")));
        Assert.False(File.Exists(ArchitectureTestPaths.FromRoot("FoodDiary.Domain", "Enums", "ActivityLevel.cs")));
    }

    [Theory]
    [InlineData("User.cs")]
    [InlineData("User.Admin.cs")]
    [InlineData("User.Credentials.cs")]
    [InlineData("User.Goals.cs")]
    [InlineData("User.Lifecycle.cs")]
    [InlineData("User.Profile.cs")]
    [InlineData("User.Tdee.cs")]
    [InlineData("Role.cs")]
    [InlineData("UserRole.cs")]
    [InlineData("UserRoleAuditEvent.cs")]
    public void UsersAggregateFiles_HaveOnePhysicalOwner(string fileName) {
        Assert.True(File.Exists(ArchitectureTestPaths.FromRoot(
            "Modules", "Users", "Domain", "Entities", "Users", fileName)));
        Assert.False(File.Exists(ArchitectureTestPaths.FromRoot(
            "FoodDiary.Domain", "Entities", "Users", fileName)));
    }

    [Theory]
    [InlineData("WeightGoal")]
    [InlineData("WaistGoal")]
    public void GoalsAndMappings_BelongToUsers(string name) {
        Assert.True(File.Exists(ArchitectureTestPaths.FromRoot(
            "Modules", "Users", "Domain", "Entities", "Tracking", name + ".cs")));
        Assert.True(File.Exists(ArchitectureTestPaths.FromRoot(
            "Modules", "Users", "Infrastructure", "Model", "Persistence", "Configurations", "BodyMetrics", name + "Configuration.cs")));
        Assert.False(File.Exists(ArchitectureTestPaths.FromRoot(
            "FoodDiary.Infrastructure", "Persistence", "Configurations", "BodyMetrics", name + "Configuration.cs")));
    }

    [Theory]
    [InlineData("Admin")]
    [InlineData("Ai")]
    [InlineData("Identity")]
    [InlineData("WeeklyGoals")]
    public void IdentifierOnlyDomains_DoNotDependOnUserAggregate(string module) {
        string[] references = ProjectReferenceReader.ReadProjectReferences(
            $"Modules/{module}/Domain/FoodDiary.Modules.{module}.Domain.csproj");
        Assert.Contains("FoodDiary.Modules.Users.Domain.Contracts", references, StringComparer.Ordinal);
        Assert.DoesNotContain("FoodDiary.Modules.Users.Domain", references, StringComparer.Ordinal);
    }

    [Fact]
    public void UsersApplicationSource_LivesOnlyInExtractedAssembly() {
        string legacyRoot = ArchitectureTestPaths.FromRoot("FoodDiary.Application", "Users");
        string extractedRoot = ArchitectureTestPaths.FromRoot("Modules", "Users", "Application");

        Assert.Empty(Directory.Exists(legacyRoot) ? SourceScanner.SourceFiles(legacyRoot) : []);
        Assert.NotEmpty(SourceScanner.SourceFiles(extractedRoot));
        Assert.True(File.Exists(Path.Combine(extractedRoot, "FoodDiary.Modules.Users.Application.csproj")));
    }

    [Fact]
    public void CoreApplication_DoesNotReferenceExtractedUsersAssembly() {
        string[] references = ProjectReferenceReader.ReadProjectReferences(
            "FoodDiary.Application.Runtime/FoodDiary.Application.Runtime.csproj");

        Assert.DoesNotContain("FoodDiary.Application.Users", references, StringComparer.Ordinal);
    }

    [Fact]
    public void ExtractedUsersAssembly_DoesNotReferenceCoreApplication() {
        string[] references = ProjectReferenceReader.ReadProjectReferences(
            "Modules/Users/Application/FoodDiary.Modules.Users.Application.csproj");

        Assert.DoesNotContain("FoodDiary.Application", references, StringComparer.Ordinal);
        Assert.Contains("FoodDiary.Application.Abstractions", references, StringComparer.Ordinal);
    }

    [Theory]
    [InlineData("FoodDiary.Web.Api/Extensions/ApiServiceCollectionExtensions.cs")]
    [InlineData("FoodDiary.JobManager/Program.cs")]
    [InlineData("FoodDiary.Initializer/Program.cs")]
    public void ExecutableCompositionRoots_RegisterUsersModule(string relativePath) {
        string source = File.ReadAllText(ArchitectureTestPaths.FromRoot(relativePath.Split('/')));

        Assert.Contains("AddUsersModule()", source, StringComparison.Ordinal);
    }
}
