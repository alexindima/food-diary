namespace FoodDiary.ArchitectureTests;

public class FeatureStructureTests
{
    [Fact]
    public void Application_Features_HaveCommandsOrQueriesFolders()
    {
        var root = GetRepositoryRoot();
        var applicationPath = Path.Combine(root, "FoodDiary.Application");
        var excluded = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "bin", "obj", "Common" };

        var featureDirectories = Directory.GetDirectories(applicationPath)
            .Select(Path.GetFileName)
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Select(name => name!)
            .Where(name => !excluded.Contains(name))
            .ToArray();

        Assert.NotEmpty(featureDirectories);

        foreach (var feature in featureDirectories)
        {
            var featurePath = Path.Combine(applicationPath, feature);
            var hasCommands = Directory.Exists(Path.Combine(featurePath, "Commands"));
            var hasQueries = Directory.Exists(Path.Combine(featurePath, "Queries"));
            Assert.True(hasCommands || hasQueries, $"Feature '{feature}' should contain Commands and/or Queries folder.");
        }
    }

    [Fact]
    public void WebApi_FeatureFolders_ContainControllers()
    {
        var root = GetRepositoryRoot();
        var featuresPath = Path.Combine(root, "FoodDiary.Web.Api", "Features");
        var featureDirectories = Directory.GetDirectories(featuresPath);

        Assert.NotEmpty(featureDirectories);

        foreach (var featurePath in featureDirectories)
        {
            var controllers = Directory.GetFiles(featurePath, "*Controller.cs");
            Assert.NotEmpty(controllers);
        }
    }

    private static string GetRepositoryRoot()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current is not null)
        {
            var solutionPath = Path.Combine(current.FullName, "FoodDiary.sln");
            if (File.Exists(solutionPath))
            {
                return current.FullName;
            }

            current = current.Parent;
        }

        throw new InvalidOperationException("Repository root was not found.");
    }
}
