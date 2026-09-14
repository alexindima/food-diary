using System.Reflection;
using FoodDiary.Presentation.Api.Controllers;

namespace FoodDiary.Presentation.Api.Tests;

[ExcludeFromCodeCoverage]
internal static class PresentationTestDiscovery {
    internal static readonly Assembly[] Assemblies = DiscoverAssemblies();

    internal static IEnumerable<Type> GetTypes() => Assemblies.SelectMany(static assembly => assembly.GetTypes());

    internal static bool IsPresentationAssembly(Assembly assembly) => Assemblies.Contains(assembly);

    internal static string[] GetPresentationRoots() {
        string repositoryRoot = GetRepositoryRoot();
        return [
            Path.Combine(repositoryRoot, "FoodDiary.Presentation.Api"),
            .. Directory.GetDirectories(Path.Combine(repositoryRoot, "Modules"), "Presentation", SearchOption.AllDirectories)
                .Where(static path => Directory.GetFiles(path, "*.Presentation.csproj", SearchOption.TopDirectoryOnly).Length == 1)
                .Order(StringComparer.Ordinal),
        ];
    }

    internal static string GetFeatureSourceRoot(string presentationRoot) =>
        (File.Exists(Path.Combine(presentationRoot, "FoodDiary.Modules.Admin.Presentation.csproj"))
            || File.Exists(Path.Combine(presentationRoot, "FoodDiary.Modules.Ai.Presentation.csproj")))
            ? presentationRoot : Path.Combine(presentationRoot, "Features");

    internal static string AdminPresentationRoot => Path.Combine(GetRepositoryRoot(), "Modules", "Admin", "Presentation");

    internal static string AiPresentationRoot => Path.Combine(GetRepositoryRoot(), "Modules", "Ai", "Presentation");

    private static Assembly[] DiscoverAssemblies() => [
        typeof(BaseApiController).Assembly,
        .. Directory.GetFiles(AppContext.BaseDirectory, "FoodDiary.Modules.*.Presentation.dll")
            .Order(StringComparer.Ordinal)
            .Select(Assembly.LoadFrom),
    ];

    private static string GetRepositoryRoot() {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);
        while (directory is not null) {
            if (File.Exists(Path.Combine(directory.FullName, "FoodDiary.slnx"))) {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException("Repository root was not found from the test output directory.");
    }
}
