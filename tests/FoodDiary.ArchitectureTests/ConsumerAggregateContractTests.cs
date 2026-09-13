using System.Globalization;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace FoodDiary.ArchitectureTests;

[ExcludeFromCodeCoverage]
public sealed class ConsumerAggregateContractTests {
    [Theory]
    [InlineData("Modules/Notifications/Application/Abstractions/Common/INotificationWriter.cs")]
    [InlineData("Modules/Notifications/Application/Abstractions/Common/NotificationRequest.cs")]
    [InlineData("Modules/Lessons/Contracts/Common/ILessonAdministrationService.cs")]
    [InlineData("Modules/Lessons/Contracts/Models/LessonAdminReadModel.cs")]
    [InlineData("Modules/Identity/Application/Abstractions/Admin/Common/IEmailTemplateAdministrationService.cs")]
    [InlineData("Modules/Identity/Application/Abstractions/Admin/Models/EmailTemplateReadModel.cs")]
    public void ConsumerContract_DoesNotExposeDomainEntities(string relativePath) {
        var entityNames = new HashSet<string>(StringComparer.Ordinal);
        foreach (string module in Directory.GetDirectories(ArchitectureTestPaths.FromRoot("Modules"))) {
            string entities = Path.Combine(module, "Domain", "Entities");
            if (!Directory.Exists(entities)) {
                continue;
            }

            foreach (string path in SourceScanner.SourceFiles(entities)) {
                entityNames.UnionWith(CSharpSyntaxTree.ParseText(File.ReadAllText(path)).GetRoot()
                    .DescendantNodes().OfType<TypeDeclarationSyntax>()
                    .Select(type => type.Identifier.ValueText));
            }
        }

        Assert.NotEmpty(entityNames);
        string[] violations = [.. CSharpSyntaxTree.ParseText(File.ReadAllText(ArchitectureTestPaths.FromRoot(relativePath)))
            .GetRoot().DescendantNodes().OfType<SimpleNameSyntax>()
            .Where(name => entityNames.Contains(name.Identifier.ValueText))
            .Select(name => $"{relativePath}:{(name.GetLocation().GetLineSpan().StartLinePosition.Line + 1).ToString(CultureInfo.InvariantCulture)}: {name.Identifier.ValueText}")];
        Assert.Empty(violations);
    }

    [Theory]
    [InlineData("Identity", "Notification")]
    [InlineData("Dietologist", "Notification")]
    [InlineData("Fasting", "Notification")]
    [InlineData("WeeklyGoals", "Notification")]
    [InlineData("RecipeCommunity", "Notification")]
    [InlineData("Admin", "NutritionLesson")]
    [InlineData("Admin", "EmailTemplate")]
    public void ConsumerApplication_DoesNotUseForeignAggregate(string owner, string aggregate) {
        string root = ArchitectureTestPaths.FromRoot($"Modules/{owner}/Application");
        string[] violations = [.. SourceScanner.SourceFiles(root)
            .SelectMany(path => CSharpSyntaxTree.ParseText(File.ReadAllText(path)).GetRoot()
                .DescendantNodes().OfType<SimpleNameSyntax>()
                .Where(name => string.Equals(name.Identifier.ValueText, aggregate, StringComparison.Ordinal))
                .Select(name => $"{Path.GetRelativePath(ArchitectureTestPaths.RepositoryRoot, path)}:{(name.GetLocation().GetLineSpan().StartLinePosition.Line + 1).ToString(CultureInfo.InvariantCulture)}: {name.Identifier.ValueText}"))];
        Assert.Empty(violations);
    }
}
