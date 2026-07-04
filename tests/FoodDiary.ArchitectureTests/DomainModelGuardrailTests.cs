using System.Reflection;
using FoodDiary.Domain.Common;

namespace FoodDiary.ArchitectureTests;

[ExcludeFromCodeCoverage]
public class DomainModelGuardrailTests {
    private static readonly HashSet<string> AllowedWideMutators = new(StringComparer.Ordinal);

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
    public void DomainConcreteClasses_AreSealedOrStaticExceptEfNavigationEntities() {
        string domainRoot = ArchitectureTestPaths.FromRoot("FoodDiary.Domain");

        string[] violations = SourceScanner.FindUnsealedConcreteClassDeclarations(
            [domainRoot],
            static path => !path.EndsWith(
                Path.Combine("Entities", "Meals", "MealItem.cs"),
                StringComparison.Ordinal));

        Assert.Empty(violations);
    }

    [Fact]
    public void DomainAggregates_DoNotIntroduceNewWidePublicMutators() {
        string[] violations = [.. typeof(AggregateRoot<>).Assembly
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
