using FoodDiary.Results;

namespace FoodDiary.Modules.Ai.Application.Abstractions.Common;

public static class AiErrors {
    public static Error InvalidRecipeVideo() => new(
        "Ai.InvalidRecipeVideo", "Use an MP4 or WebM video up to 50 MiB with an audio track no longer than five minutes.", Kind: ErrorKind.Validation);

    public static Error RecipeVideoUnavailable() => new(
        "Ai.RecipeVideoUnavailable", "The video could not be processed. Upload an MP4 or WebM file, or paste the recipe text.", Kind: ErrorKind.ExternalFailure);

    public static Error RecipeSourceUnavailable() => new(
        "Ai.RecipeSourceUnavailable", "The recipe source could not be read. Paste the caption or recipe text instead.",
        Kind: ErrorKind.ExternalFailure);

    public static Error InvalidRecipeUrl() => new(
        "Ai.InvalidRecipeUrl", "Use a public HTTPS recipe URL without credentials or a custom port.", Kind: ErrorKind.Validation);

    public static Error RecipeNotFound() => new(
        "Ai.RecipeNotFound", "No recipe ingredients were found in the source.", Kind: ErrorKind.Validation);

    public static Error RecognitionNotFound() => new(
        "Ai.RecognitionNotFound", "Recognition task was not found.", Kind: ErrorKind.NotFound);

    public static Error RecognitionInProgress() => new(
        "Ai.RecognitionInProgress", "An active recognition task cannot be deleted.", Kind: ErrorKind.Conflict);

    public static Error RecognitionConflict() => new(
        "Ai.RecognitionConflict", "Task identifier is already in use.", Kind: ErrorKind.Conflict);

    public static Error RecognitionQueueFull() => new(
        "Ai.RecognitionQueueFull", "Two recognition tasks are already pending.", Kind: ErrorKind.RateLimited);

    public static Error ImageNotFound(Guid id) => new(
        "Ai.ImageNotFound",
        $"Image asset with ID {id} was not found.",
        Kind: ErrorKind.NotFound);

    public static Error Forbidden() => new(
        "Ai.Forbidden",
        "Image asset does not belong to the current user.",
        Kind: ErrorKind.Forbidden);

    public static Error ConsentRequired() => new(
        "Ai.ConsentRequired",
        "AI consent must be accepted before using AI features.",
        Kind: ErrorKind.Forbidden);

    public static Error EmptyItems() => new(
        "Ai.EmptyItems",
        "No food items were provided.",
        Kind: ErrorKind.Validation);

    public static Error OpenAiFailed(string reason) => new(
        "Ai.OpenAiFailed",
        reason,
        Kind: ErrorKind.ExternalFailure);

    public static Error InvalidResponse(string reason) => new(
        "Ai.InvalidResponse",
        reason,
        Kind: ErrorKind.ExternalFailure);

    public static Error QuotaExceeded() => new(
        "Ai.QuotaExceeded",
        "AI token quota exceeded for the current month.",
        Kind: ErrorKind.RateLimited);
}
