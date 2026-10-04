namespace FoodDiary.Modules.Ai.Application.Abstractions.Common;

public sealed record RecipeAudio(byte[] Wav, double DurationSeconds, string? SourceUrl);
