namespace FoodDiary.Application.Abstractions.Common.Interfaces.Services;

public interface IDateTimeProvider {
    DateTime UtcNow { get; }
}
