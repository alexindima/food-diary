namespace FoodDiary.Domain.ValueObjects;

public sealed record DietologistPermissions(
    bool ShareMeals = true,
    bool ShareStatistics = true,
    bool ShareWeight = true,
    bool ShareWaist = true,
    bool ShareGoals = true,
    bool ShareHydration = true,
    bool ShareProfile = true,
    bool ShareFasting = true) {
    public static DietologistPermissions AllEnabled => new();
}
