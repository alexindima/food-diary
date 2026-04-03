using FoodDiary.Domain.Common;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Domain.ValueObjects;

public readonly record struct RecipeStepContentState(
    string? Title,
    string Instruction,
    string? ImageUrl,
    ImageAssetId? ImageAssetId) {
    private const int TitleMaxLength = 256;
    private const int InstructionMaxLength = 4000;
    private const int ImageUrlMaxLength = DomainConstants.ImageUrlMaxLength;

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

        var normalized = instruction.Trim();
        return normalized.Length > InstructionMaxLength
            ? throw new ArgumentOutOfRangeException(paramName, $"Instruction must be at most {InstructionMaxLength} characters.")
            : normalized;
    }

    private static string? NormalizeOptionalText(string? value, int maxLength, string paramName) {
        if (string.IsNullOrWhiteSpace(value)) {
            return null;
        }

        var normalized = value.Trim();
        return normalized.Length > maxLength
            ? throw new ArgumentOutOfRangeException(paramName, $"Value must be at most {maxLength} characters.")
            : normalized;
    }
}
