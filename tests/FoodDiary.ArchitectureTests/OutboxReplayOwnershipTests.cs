using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace FoodDiary.ArchitectureTests;

[ExcludeFromCodeCoverage]
public sealed class OutboxReplayOwnershipTests {
    [Fact]
    public void Coordinator_HasNoConcreteStreamTypes_AndRetainsTransactionOwnership() {
        string code = File.ReadAllText(ArchitectureTestPaths.FromRoot("FoodDiary.Infrastructure/Persistence/Outbox/OutboxDeadLetterReplayService.cs"));
        string[] identifiers = Identifiers(code);
        foreach (string concrete in new[] { "EmailOutboxMessage", "ImageObjectDeletionOutboxMessage", "NotificationWebPushOutboxMessage", "AchievementEvaluationOutboxMessage" }) {
            Assert.DoesNotContain(concrete, identifiers, StringComparer.Ordinal);
        }
        string[] literals = [.. CSharpSyntaxTree.ParseText(code).GetRoot().DescendantNodes().OfType<LiteralExpressionSyntax>().Select(node => node.Token.ValueText)];
        foreach (string name in new[] { "email", "image_object_deletion", "notification_web_push", "achievement_evaluation" }) {
            Assert.DoesNotContain(name, literals, StringComparer.Ordinal);
        }
        foreach (string operation in new[] { "BeginTransactionAsync", "MarkReplayed", "SaveChangesAsync", "CommitAsync", "OutboxReplayAudits" }) {
            Assert.Contains(operation, identifiers, StringComparer.Ordinal);
        }
    }

    [Theory]
    [InlineData("FoodDiary.Infrastructure/Persistence/Email/EmailOutboxReplayStream.cs")]
    [InlineData("Modules/Images/Infrastructure/Persistence/Images/ImageDeletionOutboxReplayStream.cs")]
    [InlineData("Modules/Notifications/Infrastructure/Persistence/WebPushOutboxReplayStream.cs")]
    [InlineData("Modules/Gamification/Infrastructure/Persistence/AchievementEvaluationOutboxReplayStream.cs")]
    public void StreamAdapter_OwnsQueriesButCannotSaveOrCommit(string path) {
        string code = File.ReadAllText(ArchitectureTestPaths.FromRoot(path));
        string[] identifiers = Identifiers(code);
        Assert.Contains("IOutboxReplayStream", identifiers, StringComparer.Ordinal);
        Assert.Contains("FromSqlInterpolated", identifiers, StringComparer.Ordinal);
        foreach (string operation in new[] { "SaveChanges", "SaveChangesAsync", "BeginTransaction", "BeginTransactionAsync", "CommitAsync", "MarkReplayed" }) {
            Assert.DoesNotContain(operation, identifiers, StringComparer.Ordinal);
        }
    }

    private static string[] Identifiers(string code) => [.. CSharpSyntaxTree.ParseText(code).GetRoot().DescendantTokens()
        .Where(token => token.RawKind == (int)SyntaxKind.IdentifierToken).Select(token => token.ValueText)];
}
