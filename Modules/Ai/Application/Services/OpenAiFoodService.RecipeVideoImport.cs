using System.Security.Cryptography;
using System.Text;
using FoodDiary.Modules.Ai.Application.Abstractions.Common;
using FoodDiary.Modules.Ai.Contracts.Models;
using FoodDiary.Modules.Users.Contracts.Models;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Results;
using Microsoft.Extensions.Logging;

namespace FoodDiary.Modules.Ai.Application.Services;

public sealed partial class OpenAiFoodService {
    public async Task<Result<RecipeImportDraftModel>> ImportRecipeVideoAsync(
        Stream? video, string? sourceUrl, string? text, UserId userId, string requestId, CancellationToken cancellationToken) {
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        deadline.CancelAfter(TimeSpan.FromMinutes(3));
        string stage = "load_profile";
        try {
            Result<UserAiProfileModel> context = await GetUserContextAsync(userId, deadline.Token);
            if (context.IsFailure) {
                return FailVideoImport(context.Error, stage);
            }
            stage = "prepare_audio";
            if (recipeVideoProcessor is null) {
                return FailVideoImport(AiErrors.RecipeVideoUnavailable(), stage, "processor_not_configured");
            }
            Result<RecipeAudio> audio = await recipeVideoProcessor.ExtractAudioAsync(video, sourceUrl, deadline.Token);
            if (audio.IsFailure) {
                return Result.Failure<RecipeImportDraftModel>(audio.Error);
            }
            // Audio cannot use the text input-token counter. This is a conservative quota estimate;
            // actual token usage replaces it when the transcription provider reports tokens.
            long estimatedTokens = (long)Math.Ceiling(audio.Value.DurationSeconds * 64);
            var budget = new AiProviderTokenBudget(estimatedTokens, estimatedTokens);
            string transcriptionId = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(requestId + ":recipe-transcription")));
            stage = "reserve_transcription_quota";
            Result reservation = await ReserveAsync(transcriptionId, userId, "recipe-transcription", context.Value, budget, deadline.Token);
            if (reservation.IsFailure) {
                return FailVideoImport(reservation.Error, stage);
            }
            stage = "transcribe_audio";
            Result<OpenAiFoodClientResponse<string>> transcript = await openAiFoodClient.TranscribeRecipeAudioAsync(audio.Value, deadline.Token);
            if (transcript.IsFailure) {
                return FailVideoImport(transcript.Error, stage);
            }
            await ReconcileAsync(transcriptionId, transcript.Value, budget);
            if (string.IsNullOrWhiteSpace(transcript.Value.Value) && string.IsNullOrWhiteSpace(text)) {
                return FailVideoImport(AiErrors.RecipeNotFound(), stage, "empty_transcript");
            }
            string sourceText = "Video speech:\n" + transcript.Value.Value + "\nCaption:\n" + (text?.Trim() ?? string.Empty);
            if (sourceText.Length > 16000) {
                return FailVideoImport(AiErrors.InvalidRecipeVideo(), stage, "transcript_with_caption_too_long");
            }
            stage = "parse_recipe";
            Result<RecipeImportDraftModel> parsed = await ParseRecipeSourceAsync(sourceText, audio.Value.SourceUrl, userId, requestId, context.Value, deadline.Token);
            return parsed.IsFailure ? FailVideoImport(parsed.Error, stage) : parsed;
        } catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested) {
            return FailVideoImport(AiErrors.RecipeVideoUnavailable(), stage, "deadline_expired");
        }
    }

    private Result<RecipeImportDraftModel> FailVideoImport(Error error, string stage, string? reason = null) {
        LogLevel level = error.Kind is ErrorKind.ExternalFailure or ErrorKind.Internal ? LogLevel.Warning : LogLevel.Information;
        logger?.Log(level, "Recipe video import failed at {Stage}: {Reason}", stage, reason ?? error.Code);
        return Result.Failure<RecipeImportDraftModel>(error);
    }
}
