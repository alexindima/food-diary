using FoodDiary.Application.Abstractions.Common.Interfaces.Services;

namespace FoodDiary.Presentation.Api.Services;

public sealed class PresentationClock(IDateTimeProvider dateTimeProvider) : IPresentationClock {
    public DateTime UtcNow => dateTimeProvider.UtcNow;
}
