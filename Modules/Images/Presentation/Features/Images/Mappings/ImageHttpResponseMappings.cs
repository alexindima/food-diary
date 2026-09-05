using FoodDiary.Application.Images.Commands.GetUploadUrl;
using FoodDiary.Application.Images.Commands.ConfirmUpload;
using FoodDiary.Presentation.Api.Features.Images.Responses;

namespace FoodDiary.Presentation.Api.Features.Images.Mappings;

public static class ImageHttpResponseMappings {
    extension(ConfirmImageUploadResult result) {
        public ConfirmImageUploadHttpResponse ToHttpResponse() =>
            new(result.AssetId, result.FileUrl);
    }

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
