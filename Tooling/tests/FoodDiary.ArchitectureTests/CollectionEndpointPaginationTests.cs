using System.Globalization;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace FoodDiary.ArchitectureTests;

[ExcludeFromCodeCoverage]
public sealed class CollectionEndpointPaginationTests {
    [Fact]
    public void HttpGetActions_DoNotClaimToReturnAllItems() {
        string root = ArchitectureTestPaths.RepositoryRoot;
        string[] presentationRoots = GetPresentationRoots(root);

        string[] violations = [.. SourceScanner.SourceFiles(presentationRoots)
            .Where(static path => path.EndsWith("Controller.cs", StringComparison.Ordinal))
            .SelectMany(path => CSharpSyntaxTree.ParseText(File.ReadAllText(path), path: path).GetRoot()
                .DescendantNodes().OfType<MethodDeclarationSyntax>()
                .Where(static method => method.Identifier.ValueText is "GetAll" or "ListAll")
                .Where(static method => method.AttributeLists.SelectMany(static list => list.Attributes)
                    .Any(static attribute => attribute.Name.ToString() is "HttpGet" or "HttpGetAttribute"))
                .Select(method => string.Create(
                    CultureInfo.InvariantCulture,
                    $"{Path.GetRelativePath(root, path).Replace('\\', '/')}:{method.GetLocation().GetLineSpan().StartLinePosition.Line + 1}#{method.Identifier.ValueText}")))
            .Order(StringComparer.Ordinal)];

        Assert.True(
            violations.Length == 0,
            $"HTTP GET action names must describe a page, range, or bounded collection instead of promising all items:{Environment.NewLine}{string.Join(Environment.NewLine, violations)}");
    }

    [Fact]
    public void HttpQueryModels_UseCanonicalPaginationNames() {
        string root = ArchitectureTestPaths.RepositoryRoot;
        string[] presentationRoots = GetPresentationRoots(root);
        string[] legacyNames = ["PageSize", "Take"];

        string[] violations = [.. SourceScanner.SourceFiles(presentationRoots)
            .Where(static path => path.EndsWith("HttpQuery.cs", StringComparison.Ordinal))
            .SelectMany(path => CSharpSyntaxTree.ParseText(File.ReadAllText(path), path: path).GetRoot()
                .DescendantNodes()
                .Where(node => node is ParameterSyntax or PropertyDeclarationSyntax)
                .Select(node => new {
                    path,
                    Name = node switch {
                        ParameterSyntax parameter => parameter.Identifier.ValueText,
                        PropertyDeclarationSyntax property => property.Identifier.ValueText,
                        _ => string.Empty,
                    },
                    Line = node.GetLocation().GetLineSpan().StartLinePosition.Line + 1,
                }))
            .Where(entry => legacyNames.Contains(entry.Name, StringComparer.OrdinalIgnoreCase))
            .Select(entry => $"{Path.GetRelativePath(root, entry.path).Replace('\\', '/')}:" +
                $"{entry.Line.ToString(CultureInfo.InvariantCulture)} uses legacy pagination name '{entry.Name}'")
            .Order(StringComparer.Ordinal)];

        Assert.True(
            violations.Length == 0,
            $"HTTP pagination must use page/limit or cursor/limit:{Environment.NewLine}{string.Join(Environment.NewLine, violations)}");
    }

    [Fact]
    public void HttpGetCollectionEndpoints_RequirePaginationOrReviewedBound() {
        string root = ArchitectureTestPaths.RepositoryRoot;
        string[] presentationRoots = GetPresentationRoots(root);
        string[] sourceFiles = [.. SourceScanner.SourceFiles(presentationRoots)];
        Dictionary<string, string> requestSources = BuildRequestSourceIndex(sourceFiles);
        var reviewedBoundedEndpoints = new HashSet<string>(StringComparer.Ordinal) {
            "Modules/Admin/Presentation/Controllers/AdminAiPromptsController.cs#GetRevisions",
            "Modules/Admin/Presentation/Controllers/AdminAiPromptsController.cs#GetScenarios",
            "Modules/Admin/Presentation/Controllers/AdminEmailTemplatesController.cs#GetRevisions",
            "Modules/Admin/Presentation/Controllers/AdminUsersController.cs#GetLoginSummary",
            "Modules/Ai/Presentation/Controllers/FoodRecognitionController.cs#List",
            "Modules/Dietologist/Presentation/Controllers/DietologistAttentionController.cs#GetAttentionSignals",
            "Modules/Dietologist/Presentation/Controllers/DietologistClientsController.cs#GetRecommendationsForClient",
            "Modules/Dietologist/Presentation/Controllers/RecommendationsController.cs#GetMyRecommendations",
            "Modules/Exercises/Presentation/Controllers/ExercisesController.cs#GetByDateRange",
            "Modules/Hydration/Presentation/Controllers/HydrationEntriesController.cs#GetByDate",
            "Modules/Identity/Presentation/Features/Auth/Controllers/TelegramOperationsController.cs#ListReady",
            "Modules/Notifications/Presentation/Controllers/NotificationPushController.cs#GetWebPushSubscriptions",
            "Modules/Notifications/Presentation/Controllers/NotificationsController.cs#GetNotifications",
            "Modules/Recipes/Presentation/Controllers/PublicRecipesController.cs#GetCategories",
            "Modules/Wearables/Presentation/Controllers/WearablesController.cs#GetConnections",
        };

        string[] violations = [.. sourceFiles
            .Where(static path => path.EndsWith("Controller.cs", StringComparison.Ordinal))
            .SelectMany(path => CSharpSyntaxTree.ParseText(File.ReadAllText(path), path: path).GetRoot()
                .DescendantNodes().OfType<MethodDeclarationSyntax>()
                .Where(IsHttpGetCollectionAction)
                .Select(method => new { path, method }))
            .Where(entry => !HasBoundedQuery(entry.method, requestSources))
            .Where(entry => !reviewedBoundedEndpoints.Contains(ToEndpointKey(root, entry.path, entry.method)))
            .Select(entry => string.Create(
                CultureInfo.InvariantCulture,
                $"{ToEndpointKey(root, entry.path, entry.method)}:{entry.method.GetLocation().GetLineSpan().StartLinePosition.Line + 1}"))
            .Order(StringComparer.Ordinal)];

        Assert.True(
            violations.Length == 0,
            $"Collection endpoints must declare bounded pagination or be explicitly reviewed:{Environment.NewLine}{string.Join(Environment.NewLine, violations)}");
    }

    private static Dictionary<string, string> BuildRequestSourceIndex(IEnumerable<string> sourceFiles) =>
        sourceFiles
            .SelectMany(path => CSharpSyntaxTree.ParseText(File.ReadAllText(path), path: path).GetRoot()
                .DescendantNodes().OfType<TypeDeclarationSyntax>()
                .Select(declaration => new {
                    Name = declaration.Identifier.ValueText,
                    Source = declaration.ToFullString(),
                }))
            .GroupBy(static entry => entry.Name, StringComparer.Ordinal)
            .ToDictionary(static group => group.Key, static group => string.Join('\n', group.Select(static entry => entry.Source)), StringComparer.Ordinal);

    private static bool IsHttpGetCollectionAction(MethodDeclarationSyntax method) {
        AttributeSyntax[] attributes = [.. method.AttributeLists.SelectMany(static list => list.Attributes)];
        bool isHttpGet = attributes.Any(static attribute =>
            attribute.Name.ToString() is "HttpGet" or "HttpGetAttribute");
        return isHttpGet && attributes.Any(static attribute =>
            attribute.Name.ToString().StartsWith("ProducesResponseType", StringComparison.Ordinal) &&
            IsCollectionType(attribute.Name.ToString()));
    }

    private static bool IsCollectionType(string attributeName) =>
        attributeName.Contains("IReadOnlyList<", StringComparison.Ordinal) ||
        attributeName.Contains("IReadOnlyCollection<", StringComparison.Ordinal) ||
        attributeName.Contains("IEnumerable<", StringComparison.Ordinal) ||
        attributeName.Contains("List<", StringComparison.Ordinal) ||
        attributeName.Contains("[]", StringComparison.Ordinal);

    private static bool HasBoundedQuery(MethodDeclarationSyntax method, IReadOnlyDictionary<string, string> requestSources) {
        foreach (ParameterSyntax parameter in method.ParameterList.Parameters) {
            bool isFromQuery = parameter.AttributeLists.SelectMany(static list => list.Attributes)
                .Any(static attribute => attribute.Name.ToString() is "FromQuery" or "FromQueryAttribute");
            if (!isFromQuery) {
                continue;
            }

            string parameterName = parameter.Identifier.ValueText;
            if (IsPaginationName(parameterName)) {
                return true;
            }

            string typeName = parameter.Type?.ToString().TrimEnd('?') ?? string.Empty;
            if (requestSources.TryGetValue(typeName, out string? source) &&
                new[] { "Page", "Limit", "Cursor" }.Any(token => source.Contains(token, StringComparison.Ordinal))) {
                return true;
            }
        }

        return false;
    }

    private static bool IsPaginationName(string name) =>
        name.Contains("page", StringComparison.OrdinalIgnoreCase) ||
        name.Contains("limit", StringComparison.OrdinalIgnoreCase) ||
        name.Contains("cursor", StringComparison.OrdinalIgnoreCase) ||
        name.Contains("before", StringComparison.OrdinalIgnoreCase);

    private static string ToEndpointKey(string root, string path, MethodDeclarationSyntax method) =>
        $"{Path.GetRelativePath(root, path).Replace('\\', '/')}#{method.Identifier.ValueText}";

    private static string[] GetPresentationRoots(string root) => [
        Path.Combine(root, "FoodDiary.Presentation.Api"),
        .. Directory.GetDirectories(Path.Combine(root, "Modules"), "Presentation", SearchOption.AllDirectories)
            .Where(static path => Directory.GetFiles(path, "*.csproj", SearchOption.TopDirectoryOnly).Length == 1),
        .. Directory.GetDirectories(Path.Combine(root, "Services"), "*Presentation", SearchOption.AllDirectories)
            .Where(static path => Directory.GetFiles(path, "*.csproj", SearchOption.TopDirectoryOnly).Length == 1),
    ];
}
