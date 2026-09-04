namespace FoodDiary.ArchitectureTests;

[ExcludeFromCodeCoverage]
public sealed class ContentReportsModuleExtractionTests {
    [Theory]
    [InlineData(typeof(FoodDiary.Domain.Enums.ReportStatus))]
    [InlineData(typeof(FoodDiary.Domain.Enums.ReportTargetType))]
    public void ReportEnums_AreOwnedOnlyByContentReportsDomain(Type enumType) {
        Assert.Equal("FoodDiary.Modules.ContentReports.Domain", enumType.Assembly.GetName().Name);
        Assert.Equal("FoodDiary.Domain.Enums", enumType.Namespace);
        Assert.True(File.Exists(ArchitectureTestPaths.FromRoot("Modules", "ContentReports", "Domain", "Enums", $"{enumType.Name}.cs")));
        Assert.False(File.Exists(ArchitectureTestPaths.FromRoot("FoodDiary.Domain", "Enums", $"{enumType.Name}.cs")));
        Assert.False(File.Exists(ArchitectureTestPaths.FromRoot("FoodDiary.Domain/FoodDiary.Domain.csproj")));
    }

    [Fact]
    public void ContentReportsApplicationSource_LivesOnlyInExtractedAssembly() {
        string legacyRoot = ArchitectureTestPaths.FromRoot("FoodDiary.Application", "ContentReports");
        string extractedRoot = ArchitectureTestPaths.FromRoot("Modules", "ContentReports", "Application");
        Assert.Empty(Directory.Exists(legacyRoot) ? SourceScanner.SourceFiles(legacyRoot) : []);
        Assert.NotEmpty(SourceScanner.SourceFiles(extractedRoot));
    }

    [Fact]
    public void ExtractedContentReportsAssembly_HasOnlyApprovedProjectReferences() {
        string[] references = ProjectReferenceReader.ReadProjectReferences(
            "Modules/ContentReports/Application/FoodDiary.Modules.ContentReports.Application.csproj");
        Assert.Equal(["FoodDiary.Application.Abstractions", "FoodDiary.Mediator", "FoodDiary.Modules.ContentReports.Application.Abstractions", "FoodDiary.Modules.ContentReports.Contracts", "FoodDiary.Modules.ContentReports.Domain", "FoodDiary.Modules.Users.Contracts", "FoodDiary.Modules.Users.Domain.Contracts"], references);
    }

    [Theory]
    [InlineData("FoodDiary.Web.Api/Extensions/ApiServiceCollectionExtensions.cs")]
    [InlineData("FoodDiary.Initializer/Program.cs")]
    public void ExecutableCompositionRoots_RegisterContentReportsModule(string relativePath) {
        string source = File.ReadAllText(ArchitectureTestPaths.FromRoot(relativePath.Split('/')));
        Assert.Contains("AddContentReportsModule()", source, StringComparison.Ordinal);
    }
}
