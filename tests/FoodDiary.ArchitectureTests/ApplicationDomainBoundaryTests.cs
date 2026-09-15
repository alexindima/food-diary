namespace FoodDiary.ArchitectureTests;

[ExcludeFromCodeCoverage]
public sealed class ApplicationDomainBoundaryTests {
    [Fact]
    public void ModuleApplications_DoNotReferenceForeignAggregateDomains() {
        string modulesRoot = ArchitectureTestPaths.FromRoot("Modules");
        string[] applicationProjects = [.. Directory.GetDirectories(modulesRoot)
            .Select(module => Path.Combine(module, "Application"))
            .Where(Directory.Exists)
            .SelectMany(application => Directory.GetFiles(application, "*.csproj"))];
        Assert.NotEmpty(applicationProjects);

        var violations = new List<string>();
        foreach (string project in applicationProjects) {
            string owner = new DirectoryInfo(Path.GetDirectoryName(project)!).Parent!.Name;
            string path = Path.GetRelativePath(ArchitectureTestPaths.RepositoryRoot, project);
            violations.AddRange(ProjectReferenceReader.ReadProjectReferences(path)
                .Where(reference => reference.StartsWith("FoodDiary.Modules.", StringComparison.Ordinal)
                    && reference.EndsWith(".Domain", StringComparison.Ordinal)
                    && !string.Equals(reference, $"FoodDiary.Modules.{owner}.Domain", StringComparison.Ordinal))
                .Select(reference => $"{path} -> {reference}"));
        }

        Assert.Empty(violations.Order(StringComparer.Ordinal));
    }

    [Theory]
    [InlineData("Modules/Favorites/Application/FoodDiary.Modules.Favorites.Application.csproj", "Meals")]
    [InlineData("Modules/MealPlanning/Application/FoodDiary.Modules.MealPlanning.Application.csproj", "Meals")]
    [InlineData("Modules/Export/Application/FoodDiary.Modules.Export.Application.csproj", "Cycles")]
    [InlineData("Modules/Cycles/Contracts/FoodDiary.Modules.Cycles.Contracts.csproj", "Cycles")]
    public void ScalarConsumers_ReferenceTheirNarrowOwner(string path, string owner) {
        string[] references = ProjectReferenceReader.ReadProjectReferences(path);
        Assert.Contains($"FoodDiary.Modules.{owner}.Domain.Contracts", references, StringComparer.Ordinal);
        Assert.DoesNotContain($"FoodDiary.Modules.{owner}.Domain", references, StringComparer.Ordinal);
    }

    [Theory]
    [InlineData(typeof(FoodDiary.Modules.Cycles.Domain.Contracts.Enums.BleedingType))]
    [InlineData(typeof(FoodDiary.Modules.Cycles.Domain.Contracts.Enums.CycleConfidence))]
    [InlineData(typeof(FoodDiary.Modules.Cycles.Domain.Contracts.Enums.CycleConsentPurpose))]
    [InlineData(typeof(FoodDiary.Modules.Cycles.Domain.Contracts.Enums.CycleFactorType))]
    [InlineData(typeof(FoodDiary.Modules.Cycles.Domain.Contracts.Enums.CycleFlowLevel))]
    [InlineData(typeof(FoodDiary.Modules.Cycles.Domain.Contracts.Enums.CycleReproductiveState))]
    [InlineData(typeof(FoodDiary.Modules.Cycles.Domain.Contracts.Enums.CycleSymptomCategory))]
    [InlineData(typeof(FoodDiary.Modules.Cycles.Domain.Contracts.Enums.CycleTrackingGoal))]
    [InlineData(typeof(FoodDiary.Modules.Cycles.Domain.Contracts.Enums.CycleTrackingMode))]
    [InlineData(typeof(FoodDiary.Modules.Cycles.Domain.Contracts.Enums.MenstrualEpisodeStatus))]
    [InlineData(typeof(FoodDiary.Modules.Cycles.Domain.Contracts.Enums.OvulationTestResult))]
    public void PublicCycleEnums_HaveOneScalarOwner(Type type) {
        Assert.Equal("FoodDiary.Modules.Cycles.Domain.Contracts", type.Assembly.GetName().Name);
        Assert.Equal("FoodDiary.Modules.Cycles.Domain.Contracts.Enums", type.Namespace);
        Assert.True(File.Exists(ArchitectureTestPaths.FromRoot(
            $"Modules/Cycles/Domain.Contracts/Enums/{type.Name}.cs")));
        Assert.False(File.Exists(ArchitectureTestPaths.FromRoot(
            $"Modules/Cycles/Domain/Enums/{type.Name}.cs")));
    }

    [Fact]
    public void CycleScalarContract_IsDependencyFreeAndExportsOnlyPublicEnums() {
        Assert.Empty(ProjectReferenceReader.ReadProjectReferences(
            "Modules/Cycles/Domain.Contracts/FoodDiary.Modules.Cycles.Domain.Contracts.csproj"));
        Type[] exportedTypes = typeof(FoodDiary.Modules.Cycles.Domain.Contracts.Enums.CycleTrackingMode).Assembly.GetExportedTypes();
        Assert.All(exportedTypes, type => Assert.True(type.IsEnum));
        Assert.Equal(
            ["BleedingType", "CycleConfidence", "CycleConsentPurpose", "CycleFactorType", "CycleFlowLevel",
                "CycleReproductiveState", "CycleSymptomCategory", "CycleTrackingGoal", "CycleTrackingMode",
                "MenstrualEpisodeStatus", "OvulationTestResult"],
            exportedTypes.Select(type => type.Name).Order(StringComparer.Ordinal), StringComparer.Ordinal);
    }
}
