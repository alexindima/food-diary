using FoodDiary.Modules.Marketing.Application.Commands.RecordMarketingAttribution;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace FoodDiary.ArchitectureTests;

[ExcludeFromCodeCoverage]
public sealed class MarketingIdentitySemanticBoundaryTests {
    [Theory]
    [InlineData("repository.GetLandingAsync(session, session, DateTime.UtcNow, CancellationToken.None)")]
    [InlineData("repository.GetLandingAsync(visitor, visitor, DateTime.UtcNow, CancellationToken.None)")]
    [InlineData("repository.GetLandingAsync(\"opaque visitor\", session, DateTime.UtcNow, CancellationToken.None)")]
    [InlineData("repository.ExistsForUserAsync(Guid.NewGuid(), \"custom_event\", CancellationToken.None)")]
    [InlineData("MarketingAttributionEvent.Create(\"custom_event\", DateTime.UtcNow, user, session, session, \"/\")")]
    public void VisitorSessionAndUserRoles_CannotBeInterchanged(string expression) {
        Diagnostic error = Assert.Single(Compile(expression));
        Assert.Equal("CS1503", error.Id);
    }

    [Fact]
    public void OwnerRoles_ReachLookupAndCreationWithoutChangingOpaqueStrings() {
        Assert.Empty(Compile("repository.GetLandingAsync(visitor, session, DateTime.UtcNow, CancellationToken.None)"));
        Assert.Empty(Compile("MarketingAttributionEvent.Create(\"custom_event\", DateTime.UtcNow, user, visitor, session, \"/\")"));
    }

    private static Diagnostic[] Compile(string expression) {
        string source = $$"""
            using System;
            using System.Threading;
            using FoodDiary.Modules.Marketing.Domain.ValueObjects;
            using FoodDiary.Modules.Marketing.Domain.Entities.Tracking;
            using FoodDiary.Modules.Marketing.Application.Abstractions.Common;
            using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;
            public static class Consumer {
                public static object Invoke(IMarketingAttributionEventReadRepository repository, AnonymousVisitorId visitor, MarketingSessionId session, UserId user) => {{expression}};
            }
            """;
        IEnumerable<string> assemblies = ((string?)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES") ?? string.Empty)
            .Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries)
            .Append(typeof(RecordMarketingAttributionCommand).Assembly.Location);
        MetadataReference[] references = [.. assemblies.Distinct(StringComparer.OrdinalIgnoreCase)
            .Select(path => MetadataReference.CreateFromFile(path))];
        var compilation = CSharpCompilation.Create("MarketingIdentityConsumer", [CSharpSyntaxTree.ParseText(source)], references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        return [.. compilation.GetDiagnostics().Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error)];
    }
}
