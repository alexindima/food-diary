using System.Reflection;
using FoodDiary.Modules.Fasting.Application.Commands.SendFastingNotifications;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace FoodDiary.ArchitectureTests;

[ExcludeFromCodeCoverage]
public sealed class FastingReminderSemanticBoundaryTests {
    [Theory]
    [InlineData("new UserPreferenceUpdate(ReminderDelays: 12)", "CS1503")]
    [InlineData("FastingReminderSchedule.FromSettings(12)", "CS1503")]
    [InlineData("settings.FirstHours = 20", "CS0200")]
    [InlineData("schedule.DueHours.Add(36)", "CS1061")]
    public void PreferenceAndScheduleInputs_RequireGroupsAndImmutableValues(string expression, string errorId) {
        Diagnostic error = Assert.Single(Compile(expression));
        Assert.Equal(errorId, error.Id);
    }

    [Fact]
    public void PartialInputsAndOrderedSettings_AreValidOwnerValues() {
        Assert.Empty(Compile("new UserPreferenceUpdate(new FastingReminderDelayUpdate(FirstHours: 12))"));
        Assert.Empty(Compile("FastingReminderSchedule.FromSettings(settings)"));
    }

    [Theory]
    [InlineData("FastingCheckInReminderPlanner", "GetDueReferenceIds")]
    [InlineData("FastingNotificationCandidatePlanner", "GetDueNotifications")]
    public void FastingPlanners_ConsumeOneOwnerSchedule(string typeName, string methodName) {
        Type owner = typeof(SendFastingNotificationsCommandHandler).Assembly.GetType(
            "FoodDiary.Modules.Fasting.Application.Services." + typeName, throwOnError: true)!;
        MethodInfo method = Assert.Single(owner.GetMethods(BindingFlags.Public | BindingFlags.Static),
            method => string.Equals(method.Name, methodName, StringComparison.Ordinal));
        Assert.Multiple(
            () => Assert.Equal(typeof(FastingReminderSchedule), method.GetParameters()[^1].ParameterType),
            () => Assert.DoesNotContain(method.GetParameters(), parameter => parameter.ParameterType == typeof(int)));
    }

    private static Diagnostic[] Compile(string expression) {
        string source = $$"""
            using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects;
            public static class Consumer {
                public static object Invoke(FastingReminderSettings settings, FastingReminderSchedule schedule) => {{expression}};
            }
            """;
        MetadataReference[] references = [.. ((string?)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES") ?? string.Empty)
            .Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries).Append(typeof(UserPreferenceUpdate).Assembly.Location)
            .Distinct(StringComparer.OrdinalIgnoreCase).Select(path => MetadataReference.CreateFromFile(path))];
        var compilation = CSharpCompilation.Create("FastingReminderConsumer", [CSharpSyntaxTree.ParseText(source)], references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        return [.. compilation.GetDiagnostics().Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error)];
    }
}
