namespace FoodDiary.ArchitectureTests;

[ExcludeFromCodeCoverage]
public sealed class AiProjectLayoutTests {
    [Fact]
    public void AiPersistenceModel_RemainsInInfrastructureSourceCoverage() {
        Assert.Contains(ArchitectureTestPaths.FromRoot("Modules", "Ai", "PersistenceModel"),
            ModuleSourceCatalog.InfrastructureRoots(), StringComparer.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("Application")]
    [InlineData("Application.Abstractions")]
    [InlineData("Contracts")]
    [InlineData("Domain")]
    [InlineData("Infrastructure")]
    [InlineData("PersistenceModel")]
    [InlineData("Presentation")]
    public void AiProjects_DoNotRepeatModuleFolders(string project) {
        string root = ArchitectureTestPaths.FromRoot("Modules", "Ai", project);
        Assert.True(Directory.Exists(root));
        Assert.DoesNotContain(Directory.EnumerateDirectories(root, "*", SearchOption.AllDirectories),
            directory => string.Equals(Path.GetFileName(directory), "Ai", StringComparison.OrdinalIgnoreCase)
                && !Path.GetRelativePath(root, directory).Split(Path.DirectorySeparatorChar)
                    .Any(segment => segment is "bin" or "obj" or ".artifacts"));
    }

    [Fact]
    public void AiPresentation_DoesNotWrapTheWholeProjectInFeatures() {
        string root = ArchitectureTestPaths.FromRoot("Modules", "Ai", "Presentation");
        Assert.False(Directory.Exists(Path.Combine(root, "Features")));
        Assert.Empty(Directory.EnumerateFiles(root, "*Controller.cs"));
        Assert.NotEmpty(Directory.EnumerateFiles(Path.Combine(root, "Controllers"), "*Controller.cs"));
    }

    [Fact]
    public void AiAbstractions_DoNotRepeatModuleFolder() {
        string projectDirectory = ArchitectureTestPaths.FromRoot("Modules", "Ai", "Application.Abstractions");
        Assert.DoesNotContain(Directory.EnumerateDirectories(projectDirectory),
            directory => string.Equals(Path.GetFileName(directory), "Ai", StringComparison.OrdinalIgnoreCase));
        Assert.True(Directory.Exists(Path.Combine(projectDirectory, "Common")));
        Assert.True(Directory.Exists(Path.Combine(projectDirectory, "Models")));
    }

}
