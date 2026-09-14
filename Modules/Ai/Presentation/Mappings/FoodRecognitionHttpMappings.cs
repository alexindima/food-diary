using FoodDiary.Modules.Ai.Contracts.Models;
using FoodDiary.Modules.Ai.Application.Commands.StartFoodRecognition;
using FoodDiary.Modules.Ai.Application.Queries.GetFoodRecognition;
using FoodDiary.Modules.Ai.Application.Queries.ListFoodRecognitions;
using FoodDiary.Modules.Ai.Presentation.Requests;
using FoodDiary.Modules.Ai.Presentation.Responses;

namespace FoodDiary.Modules.Ai.Presentation.Mappings;

public static class FoodRecognitionHttpMappings {
    public static GetFoodRecognitionQuery ToRecognitionQuery(this Guid id, Guid userId) => new(userId, id);

    public static ListFoodRecognitionsQuery ToRecognitionListQuery(this Guid userId) => new(userId);

    public static StartFoodRecognitionCommand ToCommand(this StartFoodRecognitionHttpRequest request, Guid userId) =>
        new(userId, request.Id, request.ImageAssetId, request.Description);

    public static FoodRecognitionJobHttpResponse ToHttpResponse(this FoodRecognitionJobModel job) =>
        new(job.Id, job.ImageAssetId, job.ImageUrl, job.Description, job.Status, job.CreatedOnUtc, job.UpdatedOnUtc,
            job.Vision?.ToHttpResponse(), job.Nutrition?.ToHttpResponse(), job.ErrorCode, job.NutritionErrorCode);
}
