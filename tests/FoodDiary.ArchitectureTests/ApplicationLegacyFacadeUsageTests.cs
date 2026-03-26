namespace FoodDiary.ArchitectureTests;

public class ApplicationLegacyFacadeUsageTests {
    [Fact]
    public void ApplicationLayer_DoesNotUse_RecipeWideUpdateFacade() {
        var repositoryRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));
        var applicationRoot = Path.Combine(repositoryRoot, "FoodDiary.Application");

        var violations = Directory
            .GetFiles(applicationRoot, "*.cs", SearchOption.AllDirectories)
            .SelectMany(file => File.ReadLines(file)
                .Select((line, index) => new { file, line, lineNumber = index + 1 }))
            .Where(entry => entry.line.Contains("recipe.Update(", StringComparison.Ordinal))
            .Select(entry => $"{entry.file}:{entry.lineNumber}")
            .ToArray();

        Assert.True(
            violations.Length == 0,
            "Application layer should use narrow recipe operations instead of recipe.Update(...)." +
            Environment.NewLine +
            string.Join(Environment.NewLine, violations));
    }

    [Fact]
    public void ApplicationLayer_DoesNotUse_ProductWideIdentityFacade() {
        var repositoryRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));
        var applicationRoot = Path.Combine(repositoryRoot, "FoodDiary.Application");

        var violations = Directory
            .GetFiles(applicationRoot, "*.cs", SearchOption.AllDirectories)
            .SelectMany(file => File.ReadLines(file)
                .Select((line, index) => new { file, line, lineNumber = index + 1 }))
            .Where(entry => entry.line.Contains("product.UpdateIdentity(", StringComparison.Ordinal))
            .Select(entry => $"{entry.file}:{entry.lineNumber}")
            .ToArray();

        Assert.True(
            violations.Length == 0,
            "Application layer should use narrow product identity operations instead of product.UpdateIdentity(...)." +
            Environment.NewLine +
            string.Join(Environment.NewLine, violations));
    }

    [Fact]
    public void ApplicationLayer_DoesNotUse_UserWideProfileFacade() {
        var repositoryRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));
        var applicationRoot = Path.Combine(repositoryRoot, "FoodDiary.Application");

        var violations = Directory
            .GetFiles(applicationRoot, "*.cs", SearchOption.AllDirectories)
            .SelectMany(file => File.ReadLines(file)
                .Select((line, index) => new { file, line, lineNumber = index + 1 }))
            .Where(entry => entry.line.Contains("user.UpdateProfile(", StringComparison.Ordinal))
            .Select(entry => $"{entry.file}:{entry.lineNumber}")
            .ToArray();

        Assert.True(
            violations.Length == 0,
            "Application layer should use narrow user profile operations instead of user.UpdateProfile(...)." +
            Environment.NewLine +
            string.Join(Environment.NewLine, violations));
    }

    [Fact]
    public void ApplicationLayer_DoesNotUse_MealWideNutritionFacade() {
        var repositoryRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));
        var applicationRoot = Path.Combine(repositoryRoot, "FoodDiary.Application");

        var violations = Directory
            .GetFiles(applicationRoot, "*.cs", SearchOption.AllDirectories)
            .Select(file => new { file, content = File.ReadAllText(file) })
            .Where(entry => entry.content.Contains("meal.ApplyNutrition(", StringComparison.Ordinal) &&
                            entry.content.Contains("isAutoCalculated:", StringComparison.Ordinal))
            .Select(entry => entry.file)
            .ToArray();

        Assert.True(
            violations.Length == 0,
            "Application layer should use compact meal nutrition updates instead of the verbose meal.ApplyNutrition(...) overload style." +
            Environment.NewLine +
            string.Join(Environment.NewLine, violations));
    }
}
