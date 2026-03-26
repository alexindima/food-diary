using System.Reflection;
using FoodDiary.Domain.Common;

namespace FoodDiary.ArchitectureTests;

public class DomainModelGuardrailTests {
    private static readonly HashSet<string> AllowedWideMutators = new(StringComparer.Ordinal);

    [Fact]
    public void DomainAggregates_DoNotIntroduceNewWidePublicMutators() {
        var violations = typeof(AggregateRoot<>).Assembly
            .GetTypes()
            .Where(IsConcreteAggregateRoot)
            .SelectMany(type => type.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)
                .Where(IsMutator)
                .Where(method => method.GetParameters().Length > 8)
                .Where(method => !AllowedWideMutators.Contains($"{method.DeclaringType!.FullName}.{method.Name}"))
                .Select(method => $"{method.DeclaringType!.FullName}.{method.Name}({method.GetParameters().Length} params)"))
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();

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
        for (var current = type; current is not null && current != typeof(object); current = current.BaseType!) {
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
