using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace FoodDiary.ArchitectureTests;

[ExcludeFromCodeCoverage]
public sealed class IndependentCommitBoundaryTests {
    [Theory]
    [InlineData("Modules/Ai/Infrastructure/Persistence/AiQuotaRepository.cs")]
    [InlineData("Modules/Ai/Infrastructure/Persistence/FoodRecognitionJobStore.cs")]
    [InlineData("Shared/FoodDiary.Outbox.Infrastructure/Persistence/OutboxMessageClaimer.cs")]
    [InlineData("Shared/FoodDiary.Persistence.Runtime/Persistence/Outbox/OutboxDeadLetterReplayService.cs")]
    public void RetriableIndependentOperations_VerifyCommitBeforeReturningTheirComputedResult(string path) {
        string source = File.ReadAllText(ArchitectureTestPaths.FromRoot(path));
        InvocationExpressionSyntax[] commits = [.. CSharpSyntaxTree.ParseText(source).GetRoot()
            .DescendantNodes().OfType<InvocationExpressionSyntax>()
            .Where(invocation => invocation.Expression is MemberAccessExpressionSyntax { Name.Identifier.ValueText: "CommitAsync" })];

        Assert.NotEmpty(commits);
        Assert.All(commits, invocation => {
            MemberAccessExpressionSyntax access = Assert.IsType<MemberAccessExpressionSyntax>(invocation.Expression);
            Assert.EndsWith("VerifiedTransactionCommit", access.Expression.ToString(), StringComparison.Ordinal);
        });
    }

    [Fact]
    public void ProviderCommitLibrary_IsDiscoveredWithoutApplicationOrRuntimeDependencies() {
        const string name = "FoodDiary.Persistence.PostgreSql";
        IReadOnlyDictionary<string, string[]> references = ProjectReferenceReader.ReadProductionProjectReferences();

        Assert.True(references.ContainsKey(name));
        Assert.Empty(references[name]);
        Assert.Contains(ArchitectureTestPaths.FromRoot("Shared", name), ModuleSourceCatalog.InfrastructureRoots(), StringComparer.Ordinal);
    }
}
