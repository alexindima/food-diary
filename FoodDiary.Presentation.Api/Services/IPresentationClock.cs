namespace FoodDiary.Presentation.Api.Services;

public interface IPresentationClock {
    DateTime UtcNow { get; }
}
