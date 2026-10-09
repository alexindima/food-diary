using FoodDiary.Modules.Identity.Application.Abstractions.Authentication.Common;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace FoodDiary.ArchitectureTests;

[ExcludeFromCodeCoverage]
public sealed class IdentityOperationSemanticBoundaryTests {
    [Theory]
    [InlineData("store.GetLeaseAsync(1, lease, lease, CancellationToken.None)")]
    [InlineData("store.GetLeaseAsync(1, operation, operation, CancellationToken.None)")]
    [InlineData("store.AcquireAsync(1, user, CancellationToken.None)")]
    [InlineData("store.AcquireAsync(1, Guid.NewGuid(), CancellationToken.None)")]
    [InlineData("store.CancelUserAsync(operation, CancellationToken.None)")]
    [InlineData("new TelegramOperationLease(operation, lease, operation, 1, \"payload\", null, DateTime.UtcNow)")]
    public void OperationLeaseAndUserRoles_CannotBeInterchanged(string expression) {
        Diagnostic error = Assert.Single(Compile(expression));
        Assert.Equal("CS1503", error.Id);
    }

    [Fact]
    public void TypedOperationAndLease_ReachTheirOwningStore() {
        Assert.Empty(Compile("store.GetLeaseAsync(1, operation, lease, CancellationToken.None)"));
        Assert.Empty(Compile("store.RegisterAsync(1, 2, user, 1, \"payload\", CancellationToken.None)"));
        Assert.Empty(Compile("new TelegramOperationLease(operation, lease, user, 1, \"payload\", null, DateTime.UtcNow)"));
    }

    private static Diagnostic[] Compile(string expression) {
        string source = $$"""
            using System;
            using System.Threading;
            using FoodDiary.Modules.Identity.Application.Abstractions.Authentication.Common;
            using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;
            public static class Consumer {
                public static object Invoke(ITelegramOperationStore store, TelegramOperationId operation, TelegramLeaseId lease, UserId user) => {{expression}};
            }
            """;
        IEnumerable<string> assemblies = ((string?)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES") ?? string.Empty)
            .Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries)
            .Append(typeof(ITelegramOperationStore).Assembly.Location);
        MetadataReference[] references = [.. assemblies.Distinct(StringComparer.OrdinalIgnoreCase)
            .Select(path => MetadataReference.CreateFromFile(path))];
        var compilation = CSharpCompilation.Create("IdentityOperationConsumer", [CSharpSyntaxTree.ParseText(source)], references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        return [.. compilation.GetDiagnostics().Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error)];
    }
}
