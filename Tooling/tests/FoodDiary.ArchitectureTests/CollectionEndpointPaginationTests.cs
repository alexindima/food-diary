using System.Globalization;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace FoodDiary.ArchitectureTests;

[ExcludeFromCodeCoverage]
public sealed class CollectionEndpointPaginationTests {
    private static readonly IReadOnlyDictionary<string, int> PaginationLimitValues =
        new Dictionary<string, int>(StringComparer.Ordinal) {
            ["MinimumPage"] = 1,
            ["MaximumPage"] = 10_000,
            ["MinimumPageSize"] = 1,
            ["MaximumPageSize"] = 100,
            ["MaximumCollectionSize"] = 1_000,
            ["MaximumRecentItems"] = 50,
            ["MaximumAdminDashboardRecentItems"] = 20,
            ["MaximumAdminMailInboxMessages"] = 200,
            ["MaximumAdminUserRoleAuditEntries"] = 50,
            ["MaximumCollaborationAuditEntries"] = 500,
            ["MaximumCursorLength"] = 128,
            ["OpenFoodFactsRequestLimits.MinimumLimit"] = 1,
            ["OpenFoodFactsRequestLimits.MaximumLimit"] = 50,
            ["ProductSuggestionRequestLimits.MinimumLimit"] = 1,
            ["ProductSuggestionRequestLimits.MaximumLimit"] = 20,
            ["UsdaRequestLimits.MinimumLimit"] = 1,
            ["UsdaRequestLimits.MaximumLimit"] = 100,
        };

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
    public void HttpQueryModels_UseSafeCanonicalPaginationShapes() {
        string root = ArchitectureTestPaths.RepositoryRoot;
        string[] presentationRoots = GetPresentationRoots(root);
        var violations = new List<string>();

        foreach (string path in SourceScanner.SourceFiles(presentationRoots)
                     .Where(static path => path.EndsWith("HttpQuery.cs", StringComparison.Ordinal))) {
            SyntaxNode syntaxRoot = CSharpSyntaxTree.ParseText(File.ReadAllText(path), path: path).GetRoot();
            foreach (TypeDeclarationSyntax declaration in syntaxRoot.DescendantNodes().OfType<TypeDeclarationSyntax>()) {
                Dictionary<string, PaginationMember> members = GetPaginationMembers(declaration);
                bool hasPage = members.ContainsKey("Page");
                bool hasCursor = members.ContainsKey("Cursor");
                bool hasLimit = members.ContainsKey("Limit");
                if (!hasPage && !hasCursor && !hasLimit) {
                    continue;
                }

                string typeLocation = ToLocation(root, path, declaration);
                if (hasPage && hasCursor) {
                    violations.Add($"{typeLocation} mixes offset and cursor pagination");
                }

                if ((hasPage || hasCursor) && !hasLimit) {
                    violations.Add($"{typeLocation} pagination requires a Limit member");
                    continue;
                }

                if (hasPage) {
                    ValidateNumericMember(root, path, members["Page"], maximum: 10_000, allowNullable: false, violations: violations);
                }

                if (hasCursor) {
                    ValidateCursorMember(root, path, members["Cursor"], violations);
                }

                if (hasLimit) {
                    int maximum = hasPage || hasCursor ? 100 : 1_000;
                    ValidateNumericMember(root, path, members["Limit"], maximum, allowNullable: !hasPage && !hasCursor, violations);
                }
            }
        }

        Assert.True(
            violations.Count == 0,
            $"HTTP query models must use page/limit, cursor/limit, or an explicitly bounded limit:{Environment.NewLine}{string.Join(Environment.NewLine, violations.Order(StringComparer.Ordinal))}");
    }

    [Fact]
    public void HttpActionPaginationParameters_DeclareSafeBounds() {
        string root = ArchitectureTestPaths.RepositoryRoot;
        string[] presentationRoots = GetPresentationRoots(root);
        var violations = new List<string>();

        foreach (string path in SourceScanner.SourceFiles(presentationRoots)
                     .Where(static path => path.EndsWith("Controller.cs", StringComparison.Ordinal))) {
            SyntaxNode syntaxRoot = CSharpSyntaxTree.ParseText(File.ReadAllText(path), path: path).GetRoot();
            foreach (ParameterSyntax parameter in syntaxRoot.DescendantNodes().OfType<MethodDeclarationSyntax>()
                         .Where(static method => method.AttributeLists.SelectMany(static list => list.Attributes)
                             .Any(static attribute => attribute.Name.ToString() is "HttpGet" or "HttpGetAttribute"))
                         .SelectMany(static method => method.ParameterList.Parameters)
                         .Where(IsDirectPaginationParameter)) {
                string name = parameter.Identifier.ValueText;
                if (name.Equals("pageSize", StringComparison.OrdinalIgnoreCase) ||
                    name.Equals("take", StringComparison.OrdinalIgnoreCase)) {
                    violations.Add($"{ToLocation(root, path, parameter)} uses legacy pagination name '{name}'");
                    continue;
                }

                var member = new PaginationMember(
                    name,
                    parameter.Type?.ToString() ?? string.Empty,
                    [.. parameter.AttributeLists.SelectMany(static list => list.Attributes)],
                    parameter);
                if (name.Equals("cursor", StringComparison.OrdinalIgnoreCase)) {
                    ValidateCursorMember(root, path, member, violations);
                } else {
                    int maximum = GetDirectParameterMaximum(name);
                    ValidateNumericMember(root, path, member, maximum, allowNullable: name.Equals("limit", StringComparison.OrdinalIgnoreCase), violations);
                }
            }
        }

        Assert.True(
            violations.Count == 0,
            $"Direct HTTP pagination parameters must declare finite safe bounds:{Environment.NewLine}{string.Join(Environment.NewLine, violations.Order(StringComparer.Ordinal))}");
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

    private static Dictionary<string, PaginationMember> GetPaginationMembers(TypeDeclarationSyntax declaration) {
        IEnumerable<PaginationMember> constructorParameters = declaration switch {
            RecordDeclarationSyntax { ParameterList: not null } record => record.ParameterList.Parameters
                .Where(static parameter => parameter.Identifier.ValueText is "Page" or "Cursor" or "Limit")
                .Select(static parameter => new PaginationMember(
                    parameter.Identifier.ValueText,
                    parameter.Type?.ToString() ?? string.Empty,
                    [.. parameter.AttributeLists.SelectMany(static list => list.Attributes)],
                    parameter)),
            _ => [],
        };
        IEnumerable<PaginationMember> properties = declaration.Members.OfType<PropertyDeclarationSyntax>()
            .Where(static property => property.Identifier.ValueText is "Page" or "Cursor" or "Limit")
            .Select(static property => new PaginationMember(
                property.Identifier.ValueText,
                property.Type.ToString(),
                [.. property.AttributeLists.SelectMany(static list => list.Attributes)],
                property));

        return constructorParameters.Concat(properties)
            .GroupBy(static member => member.Name, StringComparer.Ordinal)
            .ToDictionary(static group => group.Key, static group => group.First(), StringComparer.Ordinal);
    }

    private static void ValidateNumericMember(
        string root,
        string path,
        PaginationMember member,
        int maximum,
        bool allowNullable,
        ICollection<string> violations) {
        string expectedType = allowNullable ? "int or int?" : "int";
        if (member.Type is not "int" && !(allowNullable && string.Equals(member.Type, "int?", StringComparison.Ordinal))) {
            violations.Add($"{ToLocation(root, path, member.Syntax)} {member.Name} must be {expectedType}");
        }

        AttributeSyntax? range = member.Attributes.FirstOrDefault(static attribute =>
            attribute.Name.ToString() is "OpenApiNumericRange" or "OpenApiNumericRangeAttribute" or "Range" or "RangeAttribute");
        SeparatedSyntaxList<AttributeArgumentSyntax> arguments = range?.ArgumentList?.Arguments ?? default;
        if (arguments.Count < 2 || !TryResolvePositiveInteger(arguments[0].Expression, out int minimum) ||
            !TryResolvePositiveInteger(arguments[1].Expression, out int declaredMaximum)) {
            violations.Add($"{ToLocation(root, path, member.Syntax)} {member.Name} must declare finite numeric range bounds");
            return;
        }

        if (minimum < 1 || declaredMaximum > maximum || declaredMaximum < minimum) {
            violations.Add(
                $"{ToLocation(root, path, member.Syntax)} {member.Name} range " +
                $"{minimum.ToString(CultureInfo.InvariantCulture)}..{declaredMaximum.ToString(CultureInfo.InvariantCulture)} " +
                $"exceeds safe 1..{maximum.ToString(CultureInfo.InvariantCulture)}");
        }
    }

    private static void ValidateCursorMember(
        string root,
        string path,
        PaginationMember member,
        ICollection<string> violations) {
        if (member.Type is not "string" and not "string?") {
            violations.Add($"{ToLocation(root, path, member.Syntax)} Cursor must be string or string?");
        }

        AttributeSyntax? maxLength = member.Attributes.FirstOrDefault(static attribute =>
            attribute.Name.ToString() is "MaxLength" or "MaxLengthAttribute");
        ExpressionSyntax? argument = maxLength?.ArgumentList?.Arguments.FirstOrDefault()?.Expression;
        if (argument is null || !TryResolvePositiveInteger(argument, out int value) || value > 128) {
            violations.Add($"{ToLocation(root, path, member.Syntax)} Cursor must declare MaxLength no greater than 128");
        }
    }

    private static bool TryResolvePositiveInteger(ExpressionSyntax expression, out int value) {
        if (expression is LiteralExpressionSyntax literal && literal.Token.Value is int literalValue) {
            value = literalValue;
            return value > 0;
        }

        string expressionName = expression.ToString();
        if (PaginationLimitValues.TryGetValue(expressionName, out value) && value > 0) {
            return true;
        }

        string constantName = expression switch {
            MemberAccessExpressionSyntax memberAccess => memberAccess.Name.Identifier.ValueText,
            IdentifierNameSyntax identifier => identifier.Identifier.ValueText,
            _ => string.Empty,
        };
        return PaginationLimitValues.TryGetValue(constantName, out value) && value > 0;
    }

    private static string ToLocation(string root, string path, SyntaxNode syntax) =>
        string.Create(
            CultureInfo.InvariantCulture,
            $"{Path.GetRelativePath(root, path).Replace('\\', '/')}:{syntax.GetLocation().GetLineSpan().StartLinePosition.Line + 1}");

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

    private static bool IsDirectPaginationParameter(ParameterSyntax parameter) {
        bool isFromQuery = parameter.AttributeLists.SelectMany(static list => list.Attributes)
            .Any(static attribute => attribute.Name.ToString() is "FromQuery" or "FromQueryAttribute");
        if (!isFromQuery) {
            return false;
        }

        string name = parameter.Identifier.ValueText;
        return name.Equals("page", StringComparison.OrdinalIgnoreCase) ||
               name.Equals("limit", StringComparison.OrdinalIgnoreCase) ||
               name.Equals("pageSize", StringComparison.OrdinalIgnoreCase) ||
               name.Equals("take", StringComparison.OrdinalIgnoreCase) ||
               name.Equals("cursor", StringComparison.OrdinalIgnoreCase);
    }

    private static int GetDirectParameterMaximum(string name) {
        if (name.Equals("page", StringComparison.OrdinalIgnoreCase)) {
            return 10_000;
        }

        return name.Equals("limit", StringComparison.OrdinalIgnoreCase) ? 100 : 1_000;
    }

    private static string ToEndpointKey(string root, string path, MethodDeclarationSyntax method) =>
        $"{Path.GetRelativePath(root, path).Replace('\\', '/')}#{method.Identifier.ValueText}";

    private static string[] GetPresentationRoots(string root) => [
        Path.Combine(root, "FoodDiary.Presentation.Api"),
        .. Directory.GetDirectories(Path.Combine(root, "Modules"), "Presentation", SearchOption.AllDirectories)
            .Where(static path => Directory.GetFiles(path, "*.csproj", SearchOption.TopDirectoryOnly).Length == 1),
        .. Directory.GetDirectories(Path.Combine(root, "Services"), "*Presentation", SearchOption.AllDirectories)
            .Where(static path => Directory.GetFiles(path, "*.csproj", SearchOption.TopDirectoryOnly).Length == 1),
    ];

    [ExcludeFromCodeCoverage]
    private sealed record PaginationMember(
        string Name,
        string Type,
        AttributeSyntax[] Attributes,
        SyntaxNode Syntax);
}
