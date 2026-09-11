namespace FoodDiary.Application.Abstractions.Ai.Common;

public interface IFoodRecognitionProcessor {
    Task<bool> ProcessNextAsync(CancellationToken cancellationToken);
}
