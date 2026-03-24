using FoodDiary.Application.Images.Commands.GetUploadUrl;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Presentation.Api.Features.Images.Requests;

namespace FoodDiary.Presentation.Api.Features.Images.Mappings;

public static class ImageHttpMappings {
    public static GetImageUploadUrlCommand ToCommand(this GetImageUploadUrlHttpRequest request, UserId userId) {
        return new GetImageUploadUrlCommand(
            userId,
            request.FileName,
            request.ContentType,
            request.FileSizeBytes);
    }
}
