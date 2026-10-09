using FoodDiary.Modules.Ai.Application.Abstractions.Common;
using FoodDiary.Modules.Identity.Application.Abstractions.Authentication.Abstractions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace FoodDiary.ArchitectureTests;

[ExcludeFromCodeCoverage]
public sealed class WorkflowSemanticBoundaryTests {
    [Theory]
    [InlineData("reader.GetAsync(job, user, CancellationToken.None)")]
    [InlineData("store.DeleteCompletedAsync(job, user, CancellationToken.None)")]
    [InlineData("store.SaveVisionAsync(user, null, CancellationToken.None)")]
    [InlineData("store.CompleteAsync(user, FoodRecognitionCompletion.WithoutNutrition, CancellationToken.None)")]
    [InlineData("reader.GetAsync(Guid.NewGuid(), job, CancellationToken.None)")]
    [InlineData("new TelegramOidcAuthorizationRequest(nonce, nonce, verifier)")]
    [InlineData("new TelegramOidcAuthorizationRequest(state, verifier, verifier)")]
    [InlineData("new TelegramOidcTokenExchange(state, verifier, nonce)")]
    [InlineData("new TelegramOidcTokenExchange(code, nonce, nonce)")]
    [InlineData("validator.ValidateAsync(\"token\", state, CancellationToken.None)")]
    public void DifferentIdentityAndOidcRoles_AreRejected(string expression) {
        Diagnostic[] errors = Compile(expression);
        Assert.NotEmpty(errors);
        Assert.All(errors, error => Assert.Equal("CS1503", error.Id));
    }

    [Theory]
    [InlineData("reader.GetAsync(user, job, CancellationToken.None)")]
    [InlineData("store.DeleteCompletedAsync(user, job, CancellationToken.None)")]
    [InlineData("store.SaveVisionAsync(job, null, CancellationToken.None)")]
    [InlineData("provider.CreateAuthorizationUrl(new TelegramOidcAuthorizationRequest(state, nonce, verifier))")]
    [InlineData("provider.ExchangeAsync(new TelegramOidcTokenExchange(code, verifier, nonce), CancellationToken.None)")]
    [InlineData("validator.ValidateAsync(\"token\", nonce, CancellationToken.None)")]
    [InlineData("store.CompleteAsync(job, FoodRecognitionCompletion.WithoutNutrition, CancellationToken.None)")]
    [InlineData("store.CompleteAsync(job, FoodRecognitionCompletion.WithNutrition(nutrition), CancellationToken.None)")]
    public void CorrectIdentityAndOidcRoles_AreAccepted(string expression) => Assert.Empty(Compile(expression));

    [Theory]
    [InlineData("new FoodRecognitionCompletion(nutrition, \"vision\", \"nutrition\")", "CS1729")]
    [InlineData("FoodRecognitionCompletion.WithoutNutrition with { ErrorCode = \"vision\" }", "CS0200")]
    [InlineData("FoodRecognitionCompletion.WithoutNutrition with { Nutrition = nutrition }", "CS0200")]
    public void CompletionConstruction_CannotBypassClosedOutcomes(string expression, string code) {
        Diagnostic error = Assert.Single(Compile(expression));
        Assert.Equal(code, error.Id);
    }

    private static Diagnostic[] Compile(string expression) {
        string source = $$"""
            using System;
            using System.Threading;
            using FoodDiary.Modules.Ai.Domain.ValueObjects.Ids;
            using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;
            using FoodDiary.Modules.Ai.Application.Abstractions.Common;
            using FoodDiary.Modules.Ai.Contracts.Models;
            using FoodDiary.Modules.Identity.Application.Abstractions.Authentication.Abstractions;
            public static class Consumer {
                public static object Invoke(UserId user, FoodRecognitionJobId job,
                    IFoodRecognitionJobReader reader, IFoodRecognitionJobStore store,
                    ITelegramOidcProvider provider, ITelegramOidcTokenValidator validator,
                    TelegramAuthorizationCode code, TelegramOAuthState state,
                    TelegramOidcNonce nonce, TelegramPkceVerifier verifier, FoodNutritionModel nutrition) => {{expression}};
            }
            """;
        MetadataReference[] references = [.. ((string?)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES") ?? string.Empty)
            .Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries)
            .Append(typeof(IFoodRecognitionJobReader).Assembly.Location)
            .Append(typeof(ITelegramOidcProvider).Assembly.Location)
            .Distinct(StringComparer.OrdinalIgnoreCase).Select(path => MetadataReference.CreateFromFile(path))];
        var compilation = CSharpCompilation.Create("WorkflowConsumer", [CSharpSyntaxTree.ParseText(source)], references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        return [.. compilation.GetDiagnostics().Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error)];
    }
}
