using FoodDiary.Application.Images.Commands.GetUploadUrl;
using FoodDiary.Presentation.Api.Features.Images.Responses;

namespace FoodDiary.Presentation.Api.Features.Images.Mappings;

public static class ImageHttpResponseMappings {
    extension(GetImageUploadUrlResult result) {
        public GetImageUploadUrlHttpResponse ToHttpResponse() {
            return new GetImageUploadUrlHttpResponse(
                result.UploadUrl,
                result.FileUrl,
                result.ExpiresAtUtc,
                result.AssetId);
        }
    }
}
