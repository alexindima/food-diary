namespace FoodDiary.Application.Fasting.Services;

internal static class FastingSymptomCatalog {
    public static readonly HashSet<string> RiskySymptoms = ["dizziness", "weakness"];
    public static readonly string[] PrioritizedSymptoms = ["dizziness", "weakness", "headache", "irritability", "cravings"];
}
