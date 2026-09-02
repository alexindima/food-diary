using System.Globalization;
using System.Reflection;
using FoodDiary.Domain.Entities.Products;
using FoodDiary.Domain.Entities.Recipes;
using FoodDiary.Domain.Entities.Users;
using FoodDiary.Domain.Primitives;

namespace FoodDiary.ArchitectureTests;

[ExcludeFromCodeCoverage]
public class DomainModelGuardrailTests {
    private static readonly HashSet<string> AllowedWideMutators = new(StringComparer.Ordinal);

    [Theory]
    [InlineData("Products")]
    [InlineData("Users")]
    [InlineData("Usda")]
    [InlineData("Cycles")]
    public void DomainProject_ReferencesOnlyApprovedDomainDependencies(string module) {
        string relativeProjectPath = $"Modules/{module}/Domain/FoodDiary.Modules.{module}.Domain.csproj";

        string[] projectReferences = ProjectReferenceReader.ReadProjectReferences(relativeProjectPath);
        string[] packageReferences = ProjectReferenceReader.ReadPackageReferences(relativeProjectPath);

        Assert.All(projectReferences, reference => Assert.DoesNotContain("Application", reference, StringComparison.Ordinal));
        Assert.All(projectReferences, reference => Assert.DoesNotContain("Infrastructure", reference, StringComparison.Ordinal));
        Assert.Empty(packageReferences);
    }

    [Theory]
    [InlineData("Products")]
    [InlineData("Users")]
    [InlineData("Usda")]
    [InlineData("Cycles")]
    public void DomainRootFolders_StayLimitedToDomainModelStructure(string module) {
        string domainRoot = ArchitectureTestPaths.FromRoot("Modules", module, "Domain");
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

    [Theory]
    [InlineData("Products")]
    [InlineData("Users")]
    [InlineData("Usda")]
    [InlineData("Cycles")]
    public void DomainSourceFiles_DoNotReferenceInfrastructurePersistenceOrTransportConcerns(string module) {
        string domainRoot = ArchitectureTestPaths.FromRoot("Modules", module, "Domain");
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

    [Theory]
    [InlineData("Products")]
    [InlineData("Users")]
    [InlineData("Usda")]
    [InlineData("Cycles")]
    public void DomainSourceFiles_DoNotReferenceApplicationOrAdapterNamespaces(string module) {
        string domainRoot = ArchitectureTestPaths.FromRoot("Modules", module, "Domain");
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

    [Theory]
    [InlineData("Products")]
    [InlineData("Users")]
    [InlineData("Usda")]
    [InlineData("Cycles")]
    public void DomainStronglyTypedIds_LiveUnderValueObjectsIdsOnePerFile(string module) {
        string root = ArchitectureTestPaths.RepositoryRoot;
        string domainRoot = ArchitectureTestPaths.FromRoot("Modules", module, "Domain");
        string idsRoot = Path.Combine(domainRoot, "ValueObjects", "Ids");

        string[] violations = [.. SourceScanner.SourceFiles(domainRoot)
            .SelectMany(CSharpSyntaxReader.ReadTypeDeclarations)
            .Where(static declaration => declaration.Name.EndsWith("Id", StringComparison.Ordinal))
            .Where(declaration => !declaration.Path.StartsWith(idsRoot, StringComparison.OrdinalIgnoreCase) ||
                                  !string.Equals(Path.GetFileNameWithoutExtension(declaration.Path), declaration.Name, StringComparison.Ordinal) ||
                                  !declaration.IsRecordStruct ||
                                  !declaration.IsPublic ||
                                  !declaration.IsReadonly ||
                                  !declaration.HasGuidValuePrimaryConstructor ||
                                  !declaration.ImplementsGuidEntityId)
            .Select(declaration => string.Create(
                CultureInfo.InvariantCulture,
                $"{Path.GetRelativePath(root, declaration.Path)}:{declaration.Line}"))
            .Order(StringComparer.Ordinal)];

        Assert.Empty(violations);
    }

    [Fact]
    public void DomainAggregates_DoNotIntroduceNewWidePublicMutators() {
        string[] violations = [.. ModuleDomainAssemblyCatalog.LoadAssemblies()
            .SelectMany(assembly => assembly.GetTypes())
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

    [Fact]
    public void ModuleDomainAssemblies_CoverEveryProductionDomainProject() {
        string[] expected = [.. ProjectReferenceReader.ReadProductionProjectNames()
            .Where(name => name.StartsWith("FoodDiary.Modules.", StringComparison.Ordinal) &&
                           name.EndsWith(".Domain", StringComparison.Ordinal))
            .Order(StringComparer.Ordinal)];
        string[] actual = [.. ModuleDomainAssemblyCatalog.ReadProjectPaths()
            .Select(path => Path.GetFileNameWithoutExtension(path)!)
            .Order(StringComparer.Ordinal)];

        Assert.NotEmpty(expected);
        Assert.Equal(expected, actual);
        Assert.Equal(expected.Length, ModuleDomainAssemblyCatalog.LoadAssemblies().Distinct().Count());
    }

    [Fact]
    public void DomainAggregates_IncludeProductsRecipesAndUsersAcrossOwners() {
        Type[] aggregates = [.. ModuleDomainAssemblyCatalog.LoadAssemblies()
            .SelectMany(assembly => assembly.GetTypes())
            .Where(IsConcreteAggregateRoot)];

        Assert.Multiple(
            () => Assert.Contains(typeof(Product), aggregates),
            () => Assert.Contains(typeof(Recipe), aggregates),
            () => Assert.Contains(typeof(User), aggregates));
    }

    [Fact]
    public void ModuleDomainAssemblies_DoNotSkipUnavailableOwners() {
        var failure = new FileNotFoundException("Required module Domain assembly is unavailable.");

        FileNotFoundException actual = Assert.Throws<FileNotFoundException>(() =>
            ModuleDomainAssemblyCatalog.LoadAssemblies(name =>
                string.Equals(name.Name, typeof(Product).Assembly.GetName().Name, StringComparison.Ordinal)
                    ? throw failure
                    : Assembly.Load(name)));

        Assert.Same(failure, actual);
    }

    private static bool IsConcreteAggregateRoot(Type type) {
        return type is { IsClass: true, IsAbstract: false } &&
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
