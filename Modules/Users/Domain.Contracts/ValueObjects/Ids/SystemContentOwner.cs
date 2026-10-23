namespace FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;

public static class SystemContentOwner {
    public static UserId Id { get; } = new(new Guid("00000000-0000-4000-8000-000000000001"));
}
