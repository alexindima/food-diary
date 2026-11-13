namespace FoodDiary.Modules.Fasting.Application.Services;

internal static class FastingSymptomCatalog {
    public static readonly HashSet<string> RiskySymptoms = [with(StringComparer.Ordinal), "dizziness", "weakness"];
    public static readonly string[] PrioritizedSymptoms = ["dizziness", "weakness", "headache", "irritability", "cravings"];
}
