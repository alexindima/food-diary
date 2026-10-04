using System.Security.Cryptography;
using System.Text;
using FoodDiary.Modules.Ai.Application.Abstractions.Common;
using FoodDiary.Modules.Ai.Contracts.Models;
using FoodDiary.Modules.Users.Contracts.Models;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Results;

namespace FoodDiary.Modules.Ai.Application.Services;

public sealed partial class OpenAiFoodService {
    public async Task<Result<RecipeImportDraftModel>> ImportRecipeVideoAsync(
        Stream? video, string? sourceUrl, string? text, UserId userId, string requestId, CancellationToken cancellationToken) {
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        deadline.CancelAfter(TimeSpan.FromMinutes(3));
        try {
            Result<UserAiProfileModel> context = await GetUserContextAsync(userId, deadline.Token).ConfigureAwait(false);
            if (context.IsFailure) {
                return Result.Failure<RecipeImportDraftModel>(context.Error);
            }
            if (recipeVideoProcessor is null) {
                return Result.Failure<RecipeImportDraftModel>(AiErrors.RecipeVideoUnavailable());
            }
            Result<RecipeAudio> audio = await recipeVideoProcessor.ExtractAudioAsync(video, sourceUrl, deadline.Token).ConfigureAwait(false);
            if (audio.IsFailure) {
                return Result.Failure<RecipeImportDraftModel>(audio.Error);
            }
            // Audio cannot use the text input-token counter. This is a conservative quota estimate;
            // actual token usage replaces it when the transcription provider reports tokens.
            var budget = new AiProviderTokenBudget((long)Math.Ceiling(audio.Value.DurationSeconds * 64), 0);
            string transcriptionId = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(requestId + ":recipe-transcription")));
            Result reservation = await ReserveAsync(transcriptionId, userId, "recipe-transcription", context.Value, budget, deadline.Token).ConfigureAwait(false);
            if (reservation.IsFailure) {
                return Result.Failure<RecipeImportDraftModel>(reservation.Error);
            }
            Result<OpenAiFoodClientResponse<string>> transcript = await openAiFoodClient.TranscribeRecipeAudioAsync(audio.Value, deadline.Token).ConfigureAwait(false);
            if (transcript.IsFailure) {
                return Result.Failure<RecipeImportDraftModel>(transcript.Error);
            }
            await ReconcileAsync(transcriptionId, transcript.Value, budget).ConfigureAwait(false);
            if (string.IsNullOrWhiteSpace(transcript.Value.Value) && string.IsNullOrWhiteSpace(text)) {
                return Result.Failure<RecipeImportDraftModel>(AiErrors.RecipeNotFound());
            }
            string sourceText = "Video speech:\n" + transcript.Value.Value + "\nCaption:\n" + (text?.Trim() ?? string.Empty);
            if (sourceText.Length > 16000) {
                return Result.Failure<RecipeImportDraftModel>(AiErrors.InvalidRecipeVideo());
            }
            return await ParseRecipeSourceAsync(sourceText, audio.Value.SourceUrl, userId, requestId, context.Value, deadline.Token).ConfigureAwait(false);
        } catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested) {
            return Result.Failure<RecipeImportDraftModel>(AiErrors.RecipeVideoUnavailable());
        }
    }
}
