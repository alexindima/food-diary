using FoodDiary.Modules.Identity.Application.Authentication.Queries.GetActiveSessions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace FoodDiary.ArchitectureTests;

[ExcludeFromCodeCoverage]
public sealed class RefreshSessionSemanticBoundaryTests {
    [Theory]
    [InlineData("repository.GetByIdAsync(user, CancellationToken.None)")]
    [InlineData("repository.GetByIdAsync(Guid.NewGuid(), CancellationToken.None)")]
    [InlineData("new GetActiveSessionsQuery(user, user)")]
    [InlineData("new RevokeSessionCommand(session, session, session)")]
    [InlineData("tokens.GenerateRefreshToken(user, null, new string[0], false, user)")]
    public void SessionAndUserRoles_CannotBeInterchanged(string expression) {
        Diagnostic error = Assert.Single(Compile(expression));
        Assert.Equal("CS1503", error.Id);
    }

    [Fact]
    public void TypedLifecycleAndTokenInputs_AreValid() {
        Assert.Empty(Compile("repository.GetByIdAsync(session, CancellationToken.None)"));
        Assert.Empty(Compile("new GetActiveSessionsQuery(user, session)"));
        Assert.Empty(Compile("new RevokeSessionCommand(user, session, session)"));
        Assert.Empty(Compile("tokens.GenerateRefreshToken(user, null, new string[0], false, session)"));
    }

    private static Diagnostic[] Compile(string expression) {
        string source = $$"""
            using System;
            using System.Threading;
            using FoodDiary.Modules.Identity.Domain.ValueObjects.Ids;
            using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;
            using FoodDiary.Modules.Identity.Application.Abstractions.Authentication.Common;
            using FoodDiary.Modules.Identity.Application.Abstractions.Authentication.Abstractions;
            using FoodDiary.Modules.Identity.Application.Authentication.Queries.GetActiveSessions;
            using FoodDiary.Modules.Identity.Application.Authentication.Commands.RevokeSession;
            public static class Consumer {
                public static object Invoke(UserId user, RefreshTokenSessionId session, IRefreshTokenSessionWriteRepository repository, IJwtTokenGenerator tokens) => {{expression}};
            }
            """;
        MetadataReference[] references = [.. ((string?)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES") ?? string.Empty)
            .Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries).Append(typeof(GetActiveSessionsQuery).Assembly.Location)
            .Distinct(StringComparer.OrdinalIgnoreCase).Select(path => MetadataReference.CreateFromFile(path))];
        var compilation = CSharpCompilation.Create("RefreshSessionConsumer", [CSharpSyntaxTree.ParseText(source)], references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        return [.. compilation.GetDiagnostics().Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error)];
    }
}
