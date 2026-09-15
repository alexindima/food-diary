using FoodDiary.Modules.Images.Application.Commands.GetUploadUrl;
using FoodDiary.Modules.Images.Application.Commands.ConfirmUpload;
using FoodDiary.Modules.Images.Presentation.Responses;

namespace FoodDiary.Modules.Images.Presentation.Mappings;

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
