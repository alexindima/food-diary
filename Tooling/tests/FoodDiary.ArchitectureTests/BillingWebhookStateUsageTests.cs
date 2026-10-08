using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace FoodDiary.ArchitectureTests;

[ExcludeFromCodeCoverage]
public sealed class BillingWebhookStateUsageTests {
    [Fact]
    public void ProcessingFlows_UseTypedLifecycleStateRatherThanRawStorageStatus() {
        string root = ArchitectureTestPaths.FromRoot("Modules/Billing/Application");
        var selected = new HashSet<string>(StringComparer.Ordinal) {
            "BillingWebhookEventProcessor",
            "ProcessQueuedBillingWebhookCommandHandler",
        };
        var covered = new HashSet<string>(StringComparer.Ordinal);
        foreach (string path in SourceScanner.SourceFiles(root)) {
            SyntaxNode syntax = CSharpSyntaxTree.ParseText(File.ReadAllText(path)).GetRoot();
            foreach (ClassDeclarationSyntax declaration in syntax.DescendantNodes().OfType<ClassDeclarationSyntax>()) {
                if (!selected.Contains(declaration.Identifier.ValueText)) {
                    continue;
                }
                covered.Add(declaration.Identifier.ValueText);
                MemberAccessExpressionSyntax[] accesses = [.. declaration.DescendantNodes().OfType<MemberAccessExpressionSyntax>()];
                Assert.Contains(accesses, access => string.Equals(access.Name.Identifier.ValueText, "ProcessingState", StringComparison.Ordinal));
                Assert.DoesNotContain(accesses, access => string.Equals(access.Name.Identifier.ValueText, "Status", StringComparison.Ordinal));
                Assert.DoesNotContain(declaration.DescendantNodes().OfType<IdentifierNameSyntax>(),
                    name => string.Equals(name.Identifier.ValueText, "ProcessedStatus", StringComparison.Ordinal));
            }
        }
        Assert.NotEmpty(covered);
        Assert.Equal(selected.Order(StringComparer.Ordinal), covered.Order(StringComparer.Ordinal));
    }
}
