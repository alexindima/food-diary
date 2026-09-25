namespace FoodDiary.Modules.Ai.Application.Common.Validation;

internal static class RecognitionImagesValidation {
    public static bool IsValid(Guid primary, bool isProductLabel, IReadOnlyList<Guid>? additional) =>
        primary != Guid.Empty && (additional is null ||
            (additional.Count <= 4 && (isProductLabel || additional.Count == 0)
                && additional.All(id => id != Guid.Empty && id != primary)
                && additional.Distinct().Count() == additional.Count));
}
