using FoodDiary.Application.Images.Commands.GetUploadUrl;
using FoodDiary.Presentation.Api.Features.Images.Responses;

namespace FoodDiary.Presentation.Api.Features.Images.Mappings;

public static class ImageHttpResponseMappings {
    public static GetImageUploadUrlHttpResponse ToHttpResponse(this GetImageUploadUrlResult result) {
        return new GetImageUploadUrlHttpResponse(
            result.UploadUrl,
            result.FileUrl,
            result.ObjectKey,
            result.ExpiresAtUtc,
            result.AssetId);
    }
}
