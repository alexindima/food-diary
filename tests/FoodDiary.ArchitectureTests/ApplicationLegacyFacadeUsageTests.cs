using System.Globalization;

namespace FoodDiary.ArchitectureTests;

[ExcludeFromCodeCoverage]
public class ApplicationLegacyFacadeUsageTests {
    [Fact]
    public void ApplicationLayer_DoesNotUse_RecipeWideUpdateFacade() {
        string repositoryRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));

        string[] violations = [.. GetApplicationSourceFiles(repositoryRoot)
            .SelectMany(file => File.ReadLines(file)
                .Select((line, index) => new { file, line, lineNumber = index + 1 }))
            .Where(entry => entry.line.Contains("recipe.Update(", StringComparison.Ordinal))
            .Select(entry => string.Create(CultureInfo.InvariantCulture, $"{entry.file}:{entry.lineNumber}"))];

        Assert.True(
            violations.Length == 0,
            "Application layer should use narrow recipe operations instead of recipe.Update(...)." +
            Environment.NewLine +
            string.Join(Environment.NewLine, violations));
    }

    [Fact]
    public void ApplicationLayer_DoesNotUse_ProductWideIdentityFacade() {
        string repositoryRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));

        string[] violations = [.. GetApplicationSourceFiles(repositoryRoot)
            .SelectMany(file => File.ReadLines(file)
                .Select((line, index) => new { file, line, lineNumber = index + 1 }))
            .Where(entry => entry.line.Contains("product.UpdateIdentity(", StringComparison.Ordinal))
            .Select(entry => string.Create(CultureInfo.InvariantCulture, $"{entry.file}:{entry.lineNumber}"))];

        Assert.True(
            violations.Length == 0,
            "Application layer should use narrow product identity operations instead of product.UpdateIdentity(...)." +
            Environment.NewLine +
            string.Join(Environment.NewLine, violations));
    }

    [Fact]
    public void ApplicationLayer_DoesNotUse_UserWideProfileFacade() {
        string repositoryRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));

        string[] violations = [.. GetApplicationSourceFiles(repositoryRoot)
            .SelectMany(file => File.ReadLines(file)
                .Select((line, index) => new { file, line, lineNumber = index + 1 }))
            .Where(entry => entry.line.Contains("user.UpdateProfile(", StringComparison.Ordinal))
            .Select(entry => $"{entry.file}:{entry.lineNumber.ToString(CultureInfo.InvariantCulture)}")];

        Assert.True(
            violations.Length == 0,
            "Application layer should use narrow user profile operations instead of user.UpdateProfile(...)." +
            Environment.NewLine +
            string.Join(Environment.NewLine, violations));
    }

    [Fact]
    public void ApplicationLayer_DoesNotUse_MealWideNutritionFacade() {
        string repositoryRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));

        string[] violations = [.. GetApplicationSourceFiles(repositoryRoot)
            .Select(file => new { file, content = File.ReadAllText(file) })
            .Where(entry => entry.content.Contains("meal.ApplyNutrition(", StringComparison.Ordinal) &&
                            entry.content.Contains("isAutoCalculated:", StringComparison.Ordinal))
            .Select(entry => entry.file)];

        Assert.True(
            violations.Length == 0,
            "Application layer should use compact meal nutrition updates instead of the verbose meal.ApplyNutrition(...) overload style." +
            Environment.NewLine +
            string.Join(Environment.NewLine, violations));
    }

    private static IEnumerable<string> GetApplicationSourceFiles(string repositoryRoot) =>
        Directory.GetDirectories(repositoryRoot, "FoodDiary.Application.*", SearchOption.TopDirectoryOnly)
            .Where(path => !path.EndsWith(".Abstractions", StringComparison.Ordinal))
            .Where(path => !path.EndsWith(".Runtime", StringComparison.Ordinal))
            .SelectMany(path => Directory.GetFiles(path, "*.cs", SearchOption.AllDirectories))
            .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase))
            .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase));
}
