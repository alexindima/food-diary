namespace FoodDiary.ArchitectureTests;

[ExcludeFromCodeCoverage]
public sealed class ExercisesModuleExtractionTests {
    [Theory]
    [InlineData("Entities/Tracking/ExerciseEntry.cs")]
    [InlineData("Enums/ExerciseType.cs")]
    [InlineData("ValueObjects/Ids/ExerciseEntryId.cs")]
    public void DomainOwnership_IsExclusiveToExercises(string relativePath) {
        Assert.False(File.Exists(ArchitectureTestPaths.FromRoot("FoodDiary.Domain", relativePath)));
        Assert.True(File.Exists(ArchitectureTestPaths.FromRoot("Modules", "Exercises", "Domain", relativePath)));
        Assert.Equal(["FoodDiary.Domain"], ProjectReferenceReader.ReadProjectReferences(
            "Modules/Exercises/Domain/FoodDiary.Modules.Exercises.Domain.csproj"));
        Assert.DoesNotContain("FoodDiary.Modules.Exercises.Domain", ProjectReferenceReader.ReadProjectReferences(
            "FoodDiary.Domain/FoodDiary.Domain.csproj"), StringComparer.Ordinal);
    }

    [Fact]
    public void PersistenceModel_IsExplicitAndDoesNotReverseInfrastructureDependency() {
        string context = File.ReadAllText(ArchitectureTestPaths.FromRoot(
            "FoodDiary.Infrastructure", "Persistence", "FoodDiaryDbContext.cs"));
        Assert.Contains("ApplyExercisesPersistenceModel()", context, StringComparison.Ordinal);
        string[] references = ProjectReferenceReader.ReadProjectReferences(
            "FoodDiary.Infrastructure/FoodDiary.Infrastructure.csproj");
        Assert.Contains("FoodDiary.Modules.Exercises.PersistenceModel", references, StringComparer.Ordinal);
        Assert.DoesNotContain("FoodDiary.Modules.Exercises.Infrastructure", references, StringComparer.Ordinal);
    }

    [Fact]
    public void FocusedTests_HaveNoDonorCopies() {
        Assert.False(Directory.Exists(ArchitectureTestPaths.FromRoot("tests", "FoodDiary.Application.Tests", "Exercises")));
        Assert.False(File.Exists(ArchitectureTestPaths.FromRoot("tests", "FoodDiary.Domain.Tests", "Domain", "ExerciseEntryInvariantTests.cs")));
        Assert.False(File.Exists(ArchitectureTestPaths.FromRoot(
            "tests", "FoodDiary.Domain.Tests", "Domain", "TrackingEntryInvariantTests.cs")));
    }

    [Fact]
    public void ExercisesApplicationSource_LivesOnlyInExtractedAssembly() {
        string legacyRoot = ArchitectureTestPaths.FromRoot("FoodDiary.Application", "Exercises");
        string extractedRoot = ArchitectureTestPaths.FromRoot("Modules", "Exercises", "Application");
        Assert.Empty(Directory.Exists(legacyRoot) ? SourceScanner.SourceFiles(legacyRoot) : []);
        Assert.NotEmpty(SourceScanner.SourceFiles(extractedRoot));
    }

    [Fact]
    public void ExtractedExercisesAssembly_HasOnlyApprovedProjectReferences() {
        string[] references = ProjectReferenceReader.ReadProjectReferences(
            "Modules/Exercises/Application/FoodDiary.Application.Exercises.csproj");
        Assert.Equal([
            "FoodDiary.Application.Abstractions",
            "FoodDiary.Domain",
            "FoodDiary.Mediator",
            "FoodDiary.Modules.Exercises.Application.Abstractions",
            "FoodDiary.Modules.Exercises.Contracts",
            "FoodDiary.Modules.Exercises.Domain",
        ], references);
    }

    [Theory]
    [InlineData("FoodDiary.Web.Api/Extensions/ApiServiceCollectionExtensions.cs")]
    [InlineData("FoodDiary.Initializer/Program.cs")]
    public void ExecutableCompositionRoots_RegisterExercisesModule(string relativePath) {
        string source = File.ReadAllText(ArchitectureTestPaths.FromRoot(relativePath.Split('/')));
        Assert.Contains("AddExercisesModule()", source, StringComparison.Ordinal);
    }
}
