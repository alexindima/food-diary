using FoodDiary.Modules.Images.Application.Commands.GetUploadUrl;
using FoodDiary.Modules.Images.Presentation.Requests;
using Microsoft.Extensions.Configuration;

namespace FoodDiary.Modules.Images.Presentation.Mappings;

public sealed class ImageUploadHttpProcessor(IConfiguration configuration) {
    public GetImageUploadUrlCommand CreateCommand(Guid userId, GetImageUploadUrlHttpRequest request) =>
        request.ToCommand(userId, configuration["RecipeModeration:Enabled"] is "true" or "True" or "1");
}
