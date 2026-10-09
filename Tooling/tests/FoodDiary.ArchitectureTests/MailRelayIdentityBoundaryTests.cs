using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace FoodDiary.ArchitectureTests;

[ExcludeFromCodeCoverage]
public sealed class MailRelayIdentityBoundaryTests {
    [Theory]
    [InlineData("store.MarkOutboxPublishedAsync(email, CancellationToken.None)")]
    [InlineData("store.MarkSentAsync(outbox, 1, CancellationToken.None)")]
    [InlineData("store.RenewClaimAsync(outbox, 1, CancellationToken.None)")]
    [InlineData("store.MarkOutboxPublishedAsync(Guid.NewGuid(), CancellationToken.None)")]
    public void ActiveQueueAndOutboxRoles_AreNotInterchangeable(string expression) {
        Diagnostic error = Assert.Single(Compile(expression));
        Assert.Equal("CS1503", error.Id);
    }

    [Theory]
    [InlineData("store.MarkOutboxPublishedAsync(outbox, CancellationToken.None)")]
    [InlineData("store.MarkSentAsync(email, 1, CancellationToken.None)")]
    [InlineData("store.RenewClaimAsync(email, 1, CancellationToken.None)")]
    public void CorrectActiveRoles_Compile(string expression) => Assert.Empty(Compile(expression));

    private static Diagnostic[] Compile(string expression) {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "FoodDiary.slnx"))) {
            directory = directory.Parent;
        }
        string root = directory?.FullName ?? throw new DirectoryNotFoundException("Repository root not found.");
        string service = Path.Combine(root, "Services", "MailRelay");
        string portPath = Path.Combine(service, "FoodDiary.MailRelay.Application", "Abstractions", "IMailRelayQueueStore.cs");
        InterfaceDeclarationSyntax port = CSharpSyntaxTree.ParseText(File.ReadAllText(portPath)).GetRoot()
            .DescendantNodes().OfType<InterfaceDeclarationSyntax>().Single();
        string[] names = ["MarkOutboxPublishedAsync", "MarkSentAsync", "RenewClaimAsync"];
        MethodDeclarationSyntax[] methods = [.. port.Members.OfType<MethodDeclarationSyntax>().Where(method => names.Contains(method.Identifier.ValueText, StringComparer.Ordinal))];
        Assert.Equal(names.Length, methods.Length);
        string portSource = "namespace FoodDiary.MailRelay.Application.Abstractions { public interface IMailRelayQueueStore { "
            + string.Join(Environment.NewLine, methods.Select(method => method.ToString())) + " } }";
        string consumer = $$"""
            using FoodDiary.MailRelay.Domain.Emails;
            using FoodDiary.MailRelay.Application.Abstractions;
            public static class Consumer {
                public static object Invoke(IMailRelayQueueStore store, QueuedEmailId email, MailRelayOutboxId outbox) => {{expression}};
            }
            """;
        SyntaxTree[] trees = [
            CSharpSyntaxTree.ParseText("global using System; global using System.Threading; global using System.Threading.Tasks; global using FoodDiary.MailRelay.Domain.Emails;"),
            CSharpSyntaxTree.ParseText(File.ReadAllText(Path.Combine(service, "FoodDiary.MailRelay.Domain", "Emails", "QueuedEmailId.cs"))),
            CSharpSyntaxTree.ParseText(File.ReadAllText(Path.Combine(service, "FoodDiary.MailRelay.Domain", "Emails", "MailRelayOutboxId.cs"))),
            CSharpSyntaxTree.ParseText(portSource), CSharpSyntaxTree.ParseText(consumer),
        ];
        MetadataReference[] references = [.. ((string?)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES") ?? string.Empty)
            .Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries).Distinct(StringComparer.OrdinalIgnoreCase)
            .Select(path => MetadataReference.CreateFromFile(path))];
        var compilation = CSharpCompilation.Create("MailRelayConsumer", trees, references, new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        return [.. compilation.GetDiagnostics().Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error)];
    }
}
