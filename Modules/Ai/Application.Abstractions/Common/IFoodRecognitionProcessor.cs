namespace FoodDiary.Modules.Ai.Application.Abstractions.Common;

public interface IFoodRecognitionProcessor {
    Task<bool> ProcessNextAsync(CancellationToken cancellationToken);
}
