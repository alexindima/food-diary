using System.Globalization;
using System.Reflection;
using FoodDiary.Domain.Entities.Users;
using FoodDiary.Domain.Primitives;

namespace FoodDiary.ArchitectureTests;

[ExcludeFromCodeCoverage]
public class DomainModelGuardrailTests {
    private static readonly HashSet<string> AllowedWideMutators = new(StringComparer.Ordinal);

    [Fact]
    public void DomainProject_ReferencesOnlySharedDomainPrimitives() {
        const string relativeProjectPath = "FoodDiary.Domain/FoodDiary.Domain.csproj";

        string[] projectReferences = ProjectReferenceReader.ReadProjectReferences(relativeProjectPath);
        string[] packageReferences = ProjectReferenceReader.ReadPackageReferences(relativeProjectPath);

        Assert.Equal(["FoodDiary.Domain.Primitives"], projectReferences);
        Assert.Empty(packageReferences);
    }

    [Fact]
    public void DomainRootFolders_StayLimitedToDomainModelStructure() {
        string domainRoot = ArchitectureTestPaths.FromRoot("FoodDiary.Domain");
        string[] allowedDirectories = [
            "Common",
            "Entities",
            "Enums",
            "Events",
            "ValueObjects",
        ];

        string[] unexpectedDirectories = [.. Directory.GetDirectories(domainRoot)
            .Select(Path.GetFileName)
            .Where(name => name is not null)
            .Select(name => name!)
            .Where(name => !name.Equals("bin", StringComparison.OrdinalIgnoreCase))
            .Where(name => !name.Equals("obj", StringComparison.OrdinalIgnoreCase))
            .Where(name => !allowedDirectories.Contains(name, StringComparer.Ordinal))
            .Order(StringComparer.Ordinal)];

        Assert.Empty(unexpectedDirectories);
    }

    [Fact]
    public void DomainSourceFiles_DoNotReferenceInfrastructurePersistenceOrTransportConcerns() {
        string domainRoot = ArchitectureTestPaths.FromRoot("FoodDiary.Domain");
        string[] forbiddenPatterns = [
            "Microsoft.EntityFrameworkCore",
            "Microsoft.AspNetCore",
            "System.Net.Http",
            "IConfiguration",
            "IOptions<",
            "HttpContext",
            "IActionResult",
            "ControllerBase",
            "DbContext",
            "DbSet<",
            "Npgsql",
        ];

        string[] violations = SourceScanner.FindLinePatternViolations(domainRoot, forbiddenPatterns);

        Assert.Empty(violations);
    }

    [Fact]
    public void DomainSourceFiles_DoNotReferenceApplicationOrAdapterNamespaces() {
        string domainRoot = ArchitectureTestPaths.FromRoot("FoodDiary.Domain");
        string[] forbiddenPatterns = [
            "FoodDiary.Application",
            "FoodDiary.Infrastructure",
            "FoodDiary.Integrations",
            "FoodDiary.Presentation.Api",
            "FoodDiary.Web.Api",
            "FoodDiary.Resources",
            "FoodDiary.MailInbox",
            "FoodDiary.MailRelay",
        ];

        string[] violations = SourceScanner.FindLinePatternViolations(domainRoot, forbiddenPatterns);

        Assert.Empty(violations);
    }

    [Fact]
    public void DomainStronglyTypedIds_LiveUnderValueObjectsIdsOnePerFile() {
        string root = ArchitectureTestPaths.RepositoryRoot;
        string domainRoot = ArchitectureTestPaths.FromRoot("FoodDiary.Domain");
        string idsRoot = Path.Combine(domainRoot, "ValueObjects", "Ids");

        string[] violations = [.. SourceScanner.SourceFiles(domainRoot)
            .SelectMany(path => File.ReadLines(path)
                .Select((line, index) => new { path, index, line = line.Trim() }))
            .Where(static entry =>
                entry.line.StartsWith("public readonly record struct ", StringComparison.Ordinal) &&
                entry.line.Contains("Id(", StringComparison.Ordinal) &&
                entry.line.Contains("IEntityId<", StringComparison.Ordinal))
            .Where(entry => !entry.path.StartsWith(idsRoot, StringComparison.OrdinalIgnoreCase) ||
                            !string.Equals(
                                Path.GetFileNameWithoutExtension(entry.path),
                                entry.line.Split(' ', StringSplitOptions.RemoveEmptyEntries)[4].Split('(')[0],
                                StringComparison.Ordinal))
            .Select(entry => string.Create(
                CultureInfo.InvariantCulture,
                $"{Path.GetRelativePath(root, entry.path)}:{entry.index + 1}"))
            .Order(StringComparer.Ordinal)];

        Assert.Empty(violations);
    }

    [Fact]
    public void DomainConcreteClasses_AreSealedOrStatic() {
        string domainRoot = ArchitectureTestPaths.FromRoot("FoodDiary.Domain");

        string[] violations = SourceScanner.FindUnsealedConcreteClassDeclarations([domainRoot]);

        Assert.Empty(violations);
    }

    [Fact]
    public void DomainAggregates_DoNotIntroduceNewWidePublicMutators() {
        string[] violations = [.. typeof(User).Assembly
            .GetTypes()
            .Where(IsConcreteAggregateRoot)
            .SelectMany(type => type.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)
                .Where(IsMutator)
                .Where(method => method.GetParameters().Length > 8)
                .Where(method => !AllowedWideMutators.Contains($"{method.DeclaringType!.FullName}.{method.Name}"))
                .Select(method => $"{method.DeclaringType!.FullName}.{method.Name}({method.GetParameters().Length} params)"))
            .Order(StringComparer.Ordinal)];

        Assert.True(
            violations.Length == 0,
            "New wide public mutators should be split into narrower domain operations. Violations:" +
            Environment.NewLine +
            string.Join(Environment.NewLine, violations));
    }

    private static bool IsConcreteAggregateRoot(Type type) {
        return type is { IsClass: true, IsAbstract: false } &&
               type.Namespace?.StartsWith("FoodDiary.Domain.Entities", StringComparison.Ordinal) == true &&
               InheritsFromGeneric(type, typeof(AggregateRoot<>));
    }

    private static bool InheritsFromGeneric(Type type, Type genericBaseType) {
        for (Type? current = type; current is not null && current != typeof(object); current = current.BaseType!) {
            if (current.IsGenericType && current.GetGenericTypeDefinition() == genericBaseType) {
                return true;
            }
        }

        return false;
    }

    private static bool IsMutator(MethodInfo method) {
        return method.ReturnType == typeof(void) &&
               (method.Name.StartsWith("Update", StringComparison.Ordinal) ||
                method.Name.StartsWith("Apply", StringComparison.Ordinal) ||
                method.Name.StartsWith("Change", StringComparison.Ordinal) ||
                method.Name.StartsWith("Set", StringComparison.Ordinal));
    }
}
