using FoodDiary.Application.Abstractions.Ai.Models;
using FoodDiary.Application.Ai.Commands.StartFoodRecognition;
using FoodDiary.Application.Ai.Queries.GetFoodRecognition;
using FoodDiary.Application.Ai.Queries.ListFoodRecognitions;
using FoodDiary.Presentation.Api.Features.Ai.Requests;
using FoodDiary.Presentation.Api.Features.Ai.Responses;

namespace FoodDiary.Presentation.Api.Features.Ai.Mappings;

public static class FoodRecognitionHttpMappings {
    public static GetFoodRecognitionQuery ToRecognitionQuery(this Guid id, Guid userId) => new(userId, id);

    public static ListFoodRecognitionsQuery ToRecognitionListQuery(this Guid userId) => new(userId);

    public static StartFoodRecognitionCommand ToCommand(this StartFoodRecognitionHttpRequest request, Guid userId) =>
        new(userId, request.Id, request.ImageAssetId, request.Description);

    public static FoodRecognitionJobHttpResponse ToHttpResponse(this FoodRecognitionJobModel job) =>
        new(job.Id, job.ImageAssetId, job.ImageUrl, job.Description, job.Status, job.CreatedOnUtc, job.UpdatedOnUtc,
            job.Vision?.ToHttpResponse(), job.Nutrition?.ToHttpResponse(), job.ErrorCode, job.NutritionErrorCode);
}
