namespace FoodDiary.ArchitectureTests;

[ExcludeFromCodeCoverage]
public sealed class ModuleErrorOwnershipTests {
    [Theory]
    [InlineData("DailyAdvices", "Common", "DailyAdviceErrors")]
    [InlineData("Dietologist", "Dietologist/Common", "DietologistErrors")]
    [InlineData("Fasting", "Common", "FastingErrors")]
    [InlineData("Hydration", "Common", "HydrationEntryErrors")]
    [InlineData("Meals", "Meals/Common", "MealErrors")]
    public void ErrorFactory_IsModuleOwnedWithoutCentralDependency(string module, string relativeFolder, string type) {
        string source = ArchitectureTestPaths.FromRoot($"Modules/{module}/Application/Abstractions/{relativeFolder}/{type}.cs");
        Assert.True(File.Exists(source), $"Missing owned factory: {source}");
        Assert.False(File.Exists(ArchitectureTestPaths.FromRoot($"FoodDiary.Application.Abstractions/{module}/Common/{type}.cs")));
        string project = $"Modules/{module}/Application/Abstractions/FoodDiary.Modules.{module}.Application.Abstractions.csproj";
        string[] references = ProjectReferenceReader.ReadProjectReferences(project);
        Assert.Contains("FoodDiary.Results", references, StringComparer.Ordinal);
        Assert.DoesNotContain("FoodDiary.Application.Contracts", references, StringComparer.Ordinal);
        string[] centralReferences = ProjectReferenceReader.ReadProjectReferences("Shared/FoodDiary.Application.Contracts/FoodDiary.Application.Contracts.csproj");
        Assert.DoesNotContain($"FoodDiary.Modules.{module}.Application.Abstractions", centralReferences, StringComparer.Ordinal);
    }
}
