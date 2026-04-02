using FoodDiary.Application.Common.Interfaces.Services;

namespace FoodDiary.Application.Common.Services;

public sealed class SystemDateTimeProvider : IDateTimeProvider {
    public DateTime UtcNow => TimeProvider.System.GetUtcNow().UtcDateTime;
}
