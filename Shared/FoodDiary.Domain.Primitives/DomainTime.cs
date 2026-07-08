namespace FoodDiary.Domain.Primitives;

public static class DomainTime {
    public static DateTime UtcNow => TimeProvider.System.GetUtcNow().UtcDateTime;
}
