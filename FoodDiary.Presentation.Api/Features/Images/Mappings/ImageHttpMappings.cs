using FoodDiary.Application.Images.Commands.GetUploadUrl;
using FoodDiary.Application.Images.Commands.DeleteImageAsset;
using FoodDiary.Application.Images.Commands.ConfirmUpload;
using FoodDiary.Presentation.Api.Features.Images.Requests;

namespace FoodDiary.Presentation.Api.Features.Images.Mappings;

public static class ImageHttpMappings {
    extension(Guid assetId) {
        public DeleteImageAssetCommand ToDeleteCommand(Guid userId) =>
                new(userId, assetId);

        public ConfirmImageUploadCommand ToConfirmCommand(Guid userId) =>
            new(userId, assetId);
    }

    extension(GetImageUploadUrlHttpRequest request) {
        public GetImageUploadUrlCommand ToCommand(Guid userId) {
            return new GetImageUploadUrlCommand(
                userId,
                request.FileName,
                request.ContentType,
                request.FileSizeBytes);
        }
    }
}
