using System.Globalization;
using FoodDiary.Domain.Common;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Domain.ValueObjects;

public readonly record struct RecipeStepContentState {
    private const int TitleMaxLength = 256;
    private const int InstructionMaxLength = 4000;
    private const int ImageUrlMaxLength = DomainConstants.ImageUrlMaxLength;

    public string? Title { get; }
    public string Instruction { get; }
    public string? ImageUrl { get; }
    public ImageAssetId? ImageAssetId { get; }

    public RecipeStepContentState(
        string? title,
        string instruction,
        string? imageUrl,
        ImageAssetId? imageAssetId) {
        Title = NormalizeOptionalText(title, TitleMaxLength, nameof(title));
        Instruction = NormalizeInstruction(instruction, nameof(instruction));
        ImageUrl = NormalizeOptionalText(imageUrl, ImageUrlMaxLength, nameof(imageUrl));
        ImageAssetId = imageAssetId;
    }

    public static RecipeStepContentState Create(
        string instruction,
        string? title = null,
        string? imageUrl = null,
        ImageAssetId? imageAssetId = null) {
        return new RecipeStepContentState(
            NormalizeOptionalText(title, TitleMaxLength, nameof(title)),
            NormalizeInstruction(instruction, nameof(instruction)),
            NormalizeOptionalText(imageUrl, ImageUrlMaxLength, nameof(imageUrl)),
            imageAssetId);
    }

    private static string NormalizeInstruction(string instruction, string paramName) {
        if (string.IsNullOrWhiteSpace(instruction)) {
            throw new ArgumentException("Instruction is required", paramName);
        }

        string normalized = instruction.Trim();
        return normalized.Length > InstructionMaxLength
            ? throw new ArgumentOutOfRangeException(paramName, $"Instruction must be at most {InstructionMaxLength} characters.")
            : normalized;
    }

    private static string? NormalizeOptionalText(string? value, int maxLength, string paramName) {
        if (string.IsNullOrWhiteSpace(value)) {
            return null;
        }

        string normalized = value.Trim();
        return normalized.Length > maxLength
            ? throw new ArgumentOutOfRangeException(paramName, string.Create(CultureInfo.InvariantCulture, $"Value must be at most {maxLength} characters."))
            : normalized;
    }
}
