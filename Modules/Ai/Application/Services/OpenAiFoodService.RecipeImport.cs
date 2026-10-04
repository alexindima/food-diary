using FoodDiary.Modules.Ai.Application.Abstractions.Common;
using FoodDiary.Modules.Ai.Contracts.Models;
using FoodDiary.Modules.Users.Contracts.Models;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Results;

namespace FoodDiary.Modules.Ai.Application.Services;

public sealed partial class OpenAiFoodService {
    public async Task<Result<RecipeImportDraftModel>> ImportRecipeAsync(
        string? sourceUrl, string? text, UserId userId, string requestId, CancellationToken cancellationToken) {
        using CancellationTokenSource deadline = CreateOperationDeadline(cancellationToken);
        try {
            Result<UserAiProfileModel> context = await GetUserContextAsync(userId, deadline.Token).ConfigureAwait(false);
            if (context.IsFailure) {
                return Result.Failure<RecipeImportDraftModel>(context.Error);
            }

            string? canonicalUrl = null;
            if (!string.IsNullOrWhiteSpace(sourceUrl)) {
                // Supplied text takes precedence, so inaccessible social posts can still be imported.
                if (!Uri.TryCreate(sourceUrl.Trim(), UriKind.Absolute, out Uri? uri) ||
                    !string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.Ordinal) || !string.IsNullOrEmpty(uri.UserInfo) || uri.Port != 443 || uri.AbsoluteUri.Length > 1400) {
                    return Result.Failure<RecipeImportDraftModel>(AiErrors.InvalidRecipeUrl());
                }
                var canonical = new UriBuilder(uri) { Fragment = string.Empty };
                if (uri.Host.Equals("instagram.com", StringComparison.OrdinalIgnoreCase) || uri.Host.Equals("www.instagram.com", StringComparison.OrdinalIgnoreCase)) {
                    canonical.Query = string.Empty;
                }
                canonicalUrl = canonical.Uri.AbsoluteUri;
            }

            string sourceText = text?.Trim() ?? string.Empty;
            if (sourceText.Length == 0) {
                if (recipeSourceReader is null || string.IsNullOrWhiteSpace(sourceUrl)) {
                    return Result.Failure<RecipeImportDraftModel>(AiErrors.RecipeSourceUnavailable());
                }
                Result<RecipeSource> source = await recipeSourceReader.ReadAsync(sourceUrl, deadline.Token).ConfigureAwait(false);
                if (source.IsFailure) {
                    return Result.Failure<RecipeImportDraftModel>(source.Error);
                }
                sourceText = source.Value.Text;
                canonicalUrl = source.Value.SourceUrl;
            }

            return await ParseRecipeSourceAsync(sourceText, canonicalUrl, userId, requestId, context.Value, deadline.Token).ConfigureAwait(false);
        } catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested) {
            return Result.Failure<RecipeImportDraftModel>(AiErrors.OpenAiFailed("Recipe import deadline expired."));
        }
    }

    private async Task<Result<RecipeImportDraftModel>> ParseRecipeSourceAsync(
        string sourceText, string? canonicalUrl, UserId userId, string requestId, UserAiProfileModel context, CancellationToken cancellationToken) {
        const string operation = "recipe-import";
        Result<AiProviderTokenBudget> budget = await openAiFoodClient.GetRecipeImportTokenBudgetAsync(
            sourceText, context.Language, cancellationToken).ConfigureAwait(false);
        if (budget.IsFailure) {
            return Result.Failure<RecipeImportDraftModel>(budget.Error);
        }
        Result reservation = await ReserveAsync(
            requestId, userId, operation, context, budget.Value, cancellationToken).ConfigureAwait(false);
        if (reservation.IsFailure) {
            return Result.Failure<RecipeImportDraftModel>(reservation.Error);
        }
        Result<OpenAiFoodClientResponse<RecipeImportDraftModel>> response = await openAiFoodClient.ImportRecipeAsync(
            sourceText, context.Language, cancellationToken).ConfigureAwait(false);
        if (response.IsFailure) {
            return Result.Failure<RecipeImportDraftModel>(response.Error);
        }
        await ReconcileAsync(requestId, response.Value, budget.Value).ConfigureAwait(false);
        if (response.Value.Value.Ingredients.Count == 0) {
            return Result.Failure<RecipeImportDraftModel>(AiErrors.RecipeNotFound());
        }
        return Result.Success(response.Value.Value with { SourceUrl = canonicalUrl });
    }
}
