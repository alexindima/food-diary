namespace FoodDiary.ArchitectureTests;

[ExcludeFromCodeCoverage]
public sealed class PhysicalProjectLayoutTests {
    // Existing physical nesting only. Remove entries as projects move; do not add new exceptions.
    private static readonly string[] LegacyNesting = [
        "Modules/Cycles/Application/FoodDiary.Application.Cycles.csproj -> Modules/Cycles/Application/Abstractions/FoodDiary.Modules.Cycles.Application.Abstractions.csproj",
        "Modules/Cycles/Infrastructure/FoodDiary.Modules.Cycles.Infrastructure.csproj -> Modules/Cycles/Infrastructure/Model/FoodDiary.Modules.Cycles.PersistenceModel.csproj",
        "Modules/DailyAdvices/Application/FoodDiary.Modules.DailyAdvices.Application.csproj -> Modules/DailyAdvices/Application/Abstractions/FoodDiary.Modules.DailyAdvices.Application.Abstractions.csproj",
        "Modules/DailyAdvices/Infrastructure/FoodDiary.Modules.DailyAdvices.Infrastructure.csproj -> Modules/DailyAdvices/Infrastructure/Model/FoodDiary.Modules.DailyAdvices.PersistenceModel.csproj",
        "Modules/Dashboard/Application/FoodDiary.Modules.Dashboard.Application.csproj -> Modules/Dashboard/Application/Abstractions/FoodDiary.Modules.Dashboard.Application.Abstractions.csproj",
        "Modules/Dietologist/Application/FoodDiary.Modules.Dietologist.Application.csproj -> Modules/Dietologist/Application/Abstractions/FoodDiary.Modules.Dietologist.Application.Abstractions.csproj",
        "Modules/Dietologist/Infrastructure/FoodDiary.Modules.Dietologist.Infrastructure.csproj -> Modules/Dietologist/Infrastructure/Model/FoodDiary.Modules.Dietologist.PersistenceModel.csproj",
        "Modules/Exercises/Application/FoodDiary.Application.Exercises.csproj -> Modules/Exercises/Application/Abstractions/FoodDiary.Modules.Exercises.Application.Abstractions.csproj",
        "Modules/Exercises/Infrastructure/FoodDiary.Modules.Exercises.Infrastructure.csproj -> Modules/Exercises/Infrastructure/Model/FoodDiary.Modules.Exercises.PersistenceModel.csproj",
        "Modules/Export/Application/FoodDiary.Modules.Export.Application.csproj -> Modules/Export/Application/Abstractions/FoodDiary.Modules.Export.Application.Abstractions.csproj",
        "Modules/Fasting/Application/FoodDiary.Modules.Fasting.Application.csproj -> Modules/Fasting/Application/Abstractions/FoodDiary.Modules.Fasting.Application.Abstractions.csproj",
        "Modules/Fasting/Infrastructure/FoodDiary.Modules.Fasting.Infrastructure.csproj -> Modules/Fasting/Infrastructure/Model/FoodDiary.Modules.Fasting.PersistenceModel.csproj",
        "Modules/Favorites/Application/FoodDiary.Application.Favorites.csproj -> Modules/Favorites/Application/Abstractions/FoodDiary.Modules.Favorites.Application.Abstractions.csproj",
        "Modules/Favorites/Infrastructure/FoodDiary.Modules.Favorites.Infrastructure.csproj -> Modules/Favorites/Infrastructure/Model/FoodDiary.Modules.Favorites.PersistenceModel.csproj",
        "Modules/Gamification/Application/FoodDiary.Modules.Gamification.Application.csproj -> Modules/Gamification/Application/Abstractions/FoodDiary.Modules.Gamification.Application.Abstractions.csproj",
        "Modules/Gamification/Infrastructure/FoodDiary.Modules.Gamification.Infrastructure.csproj -> Modules/Gamification/Infrastructure/Model/FoodDiary.Modules.Gamification.PersistenceModel.csproj",
        "Modules/Hydration/Application/FoodDiary.Modules.Hydration.Application.csproj -> Modules/Hydration/Application/Abstractions/FoodDiary.Modules.Hydration.Application.Abstractions.csproj",
        "Modules/Hydration/Infrastructure/FoodDiary.Modules.Hydration.Infrastructure.csproj -> Modules/Hydration/Infrastructure/Model/FoodDiary.Modules.Hydration.PersistenceModel.csproj",
        "Modules/Identity/Application/FoodDiary.Modules.Identity.Application.csproj -> Modules/Identity/Application/Abstractions/FoodDiary.Modules.Identity.Application.Abstractions.csproj",
        "Modules/Identity/Infrastructure/FoodDiary.Modules.Identity.Infrastructure.csproj -> Modules/Identity/Infrastructure/Model/FoodDiary.Modules.Identity.PersistenceModel.csproj",
        "Modules/Images/Application/FoodDiary.Application.Images.csproj -> Modules/Images/Application/Abstractions/FoodDiary.Modules.Images.Application.Abstractions.csproj",
        "Modules/Images/Infrastructure/FoodDiary.Modules.Images.Infrastructure.csproj -> Modules/Images/Infrastructure/Model/FoodDiary.Modules.Images.PersistenceModel.csproj",
        "Modules/Lessons/Application/FoodDiary.Modules.Lessons.Application.csproj -> Modules/Lessons/Application/Abstractions/FoodDiary.Modules.Lessons.Application.Abstractions.csproj",
        "Modules/Lessons/Infrastructure/FoodDiary.Modules.Lessons.Infrastructure.csproj -> Modules/Lessons/Infrastructure/Model/FoodDiary.Modules.Lessons.PersistenceModel.csproj",
        "Modules/Marketing/Application/FoodDiary.Application.Marketing.csproj -> Modules/Marketing/Application/Abstractions/FoodDiary.Modules.Marketing.Application.Abstractions.csproj",
        "Modules/Marketing/Infrastructure/FoodDiary.Modules.Marketing.Infrastructure.csproj -> Modules/Marketing/Infrastructure/Model/FoodDiary.Modules.Marketing.PersistenceModel.csproj",
        "Modules/MealPlanning/Application/FoodDiary.Application.MealPlanning.csproj -> Modules/MealPlanning/Application/Abstractions/FoodDiary.Modules.MealPlanning.Application.Abstractions.csproj",
        "Modules/MealPlanning/Infrastructure/FoodDiary.Modules.MealPlanning.Infrastructure.csproj -> Modules/MealPlanning/Infrastructure/Model/FoodDiary.Modules.MealPlanning.PersistenceModel.csproj",
        "Modules/Meals/Application/FoodDiary.Modules.Meals.Application.csproj -> Modules/Meals/Application/Abstractions/FoodDiary.Modules.Meals.Application.Abstractions.csproj",
        "Modules/Meals/Infrastructure/FoodDiary.Modules.Meals.Infrastructure.csproj -> Modules/Meals/Infrastructure/Model/FoodDiary.Modules.Meals.PersistenceModel.csproj",
        "Modules/Notifications/Application/FoodDiary.Application.Notifications.csproj -> Modules/Notifications/Application/Abstractions/FoodDiary.Modules.Notifications.Application.Abstractions.csproj",
        "Modules/Notifications/Infrastructure/FoodDiary.Modules.Notifications.Infrastructure.csproj -> Modules/Notifications/Infrastructure/Model/FoodDiary.Modules.Notifications.PersistenceModel.csproj",
        "Modules/OpenFoodFacts/Application/FoodDiary.Modules.OpenFoodFacts.Application.csproj -> Modules/OpenFoodFacts/Application/Abstractions/FoodDiary.Modules.OpenFoodFacts.Application.Abstractions.csproj",
        "Modules/OpenFoodFacts/Infrastructure/FoodDiary.Modules.OpenFoodFacts.Infrastructure.csproj -> Modules/OpenFoodFacts/Infrastructure/Model/FoodDiary.Modules.OpenFoodFacts.PersistenceModel.csproj",
        "Modules/Products/Application/FoodDiary.Modules.Products.Application.csproj -> Modules/Products/Application/Abstractions/FoodDiary.Modules.Products.Application.Abstractions.csproj",
        "Modules/Products/Infrastructure/FoodDiary.Modules.Products.Infrastructure.csproj -> Modules/Products/Infrastructure/Model/FoodDiary.Modules.Products.PersistenceModel.csproj",
        "Modules/RecentItems/Infrastructure/FoodDiary.Modules.RecentItems.Infrastructure.csproj -> Modules/RecentItems/Infrastructure/Model/FoodDiary.Modules.RecentItems.PersistenceModel.csproj",
        "Modules/RecipeCommunity/Application/FoodDiary.Application.RecipeCommunity.csproj -> Modules/RecipeCommunity/Application/Abstractions/FoodDiary.Modules.RecipeCommunity.Application.Abstractions.csproj",
        "Modules/RecipeCommunity/Infrastructure/FoodDiary.Modules.RecipeCommunity.Infrastructure.csproj -> Modules/RecipeCommunity/Infrastructure/Model/FoodDiary.Modules.RecipeCommunity.PersistenceModel.csproj",
        "Modules/Recipes/Application/FoodDiary.Modules.Recipes.Application.csproj -> Modules/Recipes/Application/Abstractions/FoodDiary.Modules.Recipes.Application.Abstractions.csproj",
        "Modules/Recipes/Infrastructure/FoodDiary.Modules.Recipes.Infrastructure.csproj -> Modules/Recipes/Infrastructure/Model/FoodDiary.Modules.Recipes.PersistenceModel.csproj",
        "Modules/Usda/Application/FoodDiary.Application.Usda.csproj -> Modules/Usda/Application/Abstractions/FoodDiary.Modules.Usda.Application.Abstractions.csproj",
        "Modules/Usda/Infrastructure/FoodDiary.Modules.Usda.Infrastructure.csproj -> Modules/Usda/Infrastructure/Model/FoodDiary.Modules.Usda.PersistenceModel.csproj",
        "Modules/Users/Application/FoodDiary.Modules.Users.Application.csproj -> Modules/Users/Application/Abstractions/FoodDiary.Modules.Users.Application.Abstractions.csproj",
        "Modules/Users/Infrastructure/FoodDiary.Modules.Users.Infrastructure.csproj -> Modules/Users/Infrastructure/Model/FoodDiary.Modules.Users.PersistenceModel.csproj",
        "Modules/Wearables/Application/FoodDiary.Application.Wearables.csproj -> Modules/Wearables/Application/Abstractions/FoodDiary.Modules.Wearables.Application.Abstractions.csproj",
        "Modules/Wearables/Infrastructure/FoodDiary.Modules.Wearables.Infrastructure.csproj -> Modules/Wearables/Infrastructure/Model/FoodDiary.Modules.Wearables.PersistenceModel.csproj",
        "Modules/WeeklyGoals/Application/FoodDiary.Modules.WeeklyGoals.Application.csproj -> Modules/WeeklyGoals/Application/Abstractions/FoodDiary.Modules.WeeklyGoals.Application.Abstractions.csproj",
        "Modules/WeeklyGoals/Infrastructure/FoodDiary.Modules.WeeklyGoals.Infrastructure.csproj -> Modules/WeeklyGoals/Infrastructure/Model/FoodDiary.Modules.WeeklyGoals.PersistenceModel.csproj",
    ];

    [Fact]
    public void AdminPersistenceModel_RemainsInInfrastructureSourceCoverage() {
        Assert.Contains(ArchitectureTestPaths.FromRoot("Modules", "Admin", "PersistenceModel"),
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
    public void AdminProjects_DoNotRepeatModuleFolders(string project) {
        string root = ArchitectureTestPaths.FromRoot("Modules", "Admin", project);
        Assert.True(Directory.Exists(root));
        Assert.DoesNotContain(Directory.EnumerateDirectories(root, "*", SearchOption.AllDirectories),
            directory => string.Equals(Path.GetFileName(directory), "Admin", StringComparison.OrdinalIgnoreCase)
                && !Path.GetRelativePath(root, directory).Split(Path.DirectorySeparatorChar)
                    .Any(segment => segment is "bin" or "obj" or ".artifacts"));
    }

    [Fact]
    public void AdminPresentation_DoesNotWrapTheWholeProjectInFeatures() {
        string root = ArchitectureTestPaths.FromRoot("Modules", "Admin", "Presentation");
        Assert.False(Directory.Exists(Path.Combine(root, "Features")));
        Assert.Empty(Directory.EnumerateFiles(root, "*Controller.cs"));
        Assert.NotEmpty(Directory.EnumerateFiles(Path.Combine(root, "Controllers"), "*Controller.cs"));
    }

    [Fact]
    public void AdminAbstractions_DoNotRepeatModuleFolder() {
        string projectDirectory = ArchitectureTestPaths.FromRoot("Modules", "Admin", "Application.Abstractions");
        Assert.DoesNotContain(Directory.EnumerateDirectories(projectDirectory),
            directory => string.Equals(Path.GetFileName(directory), "Admin", StringComparison.OrdinalIgnoreCase));
        Assert.True(Directory.Exists(Path.Combine(projectDirectory, "Common")));
        Assert.True(Directory.Exists(Path.Combine(projectDirectory, "Models")));
    }

    [Fact]
    public void Projects_DoNotIntroducePhysicalNesting() {
        string root = ArchitectureTestPaths.RepositoryRoot;
        string[] projects = [.. EnumerateProjectPaths(root)];
        string[] actual = FindNestedProjects(projects);
        string[] unexpected = [.. actual.Except(LegacyNesting, StringComparer.OrdinalIgnoreCase)];
        string[] stale = [.. LegacyNesting.Except(actual, StringComparer.OrdinalIgnoreCase)];
        Assert.Multiple(
            () => Assert.True(unexpected.Length == 0, "Move nested projects beside their parent projects:\n" + string.Join('\n', unexpected)),
            () => Assert.True(stale.Length == 0, "Remove resolved legacy nesting entries:\n" + string.Join('\n', stale)));
    }

    [Theory]
    [InlineData("Module/Application/P.csproj", "Module/Application/Abstractions/C.csproj", true)]
    [InlineData("Module/Application/P.csproj", "Module/Application.Abstractions/C.csproj", false)]
    [InlineData("Module/Application/P.csproj", "Module/Application/C.csproj", false)]
    [InlineData("Module/Application/P.csproj", "Module/ApplicationExtra/C.csproj", false)]
    [InlineData("Module/P.csproj", "Module/tests/Child/C.csproj", true)]
    [InlineData("P.csproj", "Child/C.csproj", true)]
    [InlineData("Module/Application/P.csproj", "module/application/Child/C.csproj", true)]
    public void NestingDetection_UsesPhysicalDirectoryBoundaries(string parent, string child, bool expected) {
        Assert.Equal(expected, FindNestedProjects([parent, child]).Length != 0);
    }

    private static string[] FindNestedProjects(string[] projects) =>
        [.. projects.SelectMany(parent => projects
            .Where(child => !string.Equals(parent, child, StringComparison.OrdinalIgnoreCase)
                && !string.Equals(Path.GetDirectoryName(parent), Path.GetDirectoryName(child), StringComparison.OrdinalIgnoreCase)
                && child.StartsWith(DirectoryPrefix(parent), StringComparison.OrdinalIgnoreCase))
            .Select(child => $"{parent} -> {child}"))
            .Order(StringComparer.OrdinalIgnoreCase)];

    private static string DirectoryPrefix(string path) {
        int separator = path.LastIndexOf('/');
        return separator < 0 ? string.Empty : path[..(separator + 1)];
    }

    private static IEnumerable<string> EnumerateProjectPaths(string root) {
        var pending = new Stack<string>();
        pending.Push(root);
        while (pending.TryPop(out string? directory)) {
            foreach (string project in Directory.EnumerateFiles(directory, "*.csproj")) {
                yield return Path.GetRelativePath(root, project).Replace('\\', '/');
            }
            foreach (string child in Directory.EnumerateDirectories(directory)) {
                string name = Path.GetFileName(child);
                if (name is ".git" or ".artifacts" or "bin" or "obj" or "node_modules" or ".angular"
                    || File.GetAttributes(child).HasFlag(FileAttributes.ReparsePoint)) {
                    continue;
                }
                pending.Push(child);
            }
        }
    }
}
