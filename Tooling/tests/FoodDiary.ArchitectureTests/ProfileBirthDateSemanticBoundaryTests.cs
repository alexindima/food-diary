using FoodDiary.Modules.Users.Domain.ValueObjects;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace FoodDiary.ArchitectureTests;

[ExcludeFromCodeCoverage]
public sealed class ProfileBirthDateSemanticBoundaryTests {
    [Theory]
    [InlineData("new UserPersonalInfoChanges(null, null, null, FieldChanges.Set(DateTime.UtcNow), null, null, null)", "CS1503")]
    [InlineData("BmrCalculationInput.FromMeasurements(null, null, DateTime.UtcNow, \"M\")", "CS1503")]
    [InlineData("birthDate.EncodedDateTime = DateTime.UtcNow", "CS0200")]
    public void SemanticInputs_RequireAnImmutableOwnerDate(string expression, string errorId) {
        Diagnostic error = Assert.Single(Compile(expression));
        Assert.Equal(errorId, error.Id);
    }

    [Fact]
    public void CalendarMeaning_ReachesFieldChangesAndCalculation() {
        Assert.Empty(Compile("new UserPersonalInfoChanges(null, null, null, FieldChanges.Set(birthDate), null, null, null)"));
        Assert.Empty(Compile("BmrCalculationInput.FromMeasurements(null, null, birthDate, \"M\")"));
    }

    private static Diagnostic[] Compile(string expression) {
        string source = $$"""
            using System;
            using FoodDiary.Domain.Primitives;
            using FoodDiary.Modules.Users.Domain.ValueObjects;
            public static class Consumer {
                public static object Invoke(ProfileBirthDate birthDate) => {{expression}};
            }
            """;
        MetadataReference[] references = [.. ((string?)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES") ?? string.Empty)
            .Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries).Append(typeof(ProfileBirthDate).Assembly.Location)
            .Distinct(StringComparer.OrdinalIgnoreCase).Select(path => MetadataReference.CreateFromFile(path))];
        var compilation = CSharpCompilation.Create("ProfileBirthDateConsumer", [CSharpSyntaxTree.ParseText(source)], references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        return [.. compilation.GetDiagnostics().Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error)];
    }
}
