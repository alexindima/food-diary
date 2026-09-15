using FoodDiary.Modules.Images.Application.Commands.GetUploadUrl;
using FoodDiary.Modules.Images.Application.Commands.DeleteImageAsset;
using FoodDiary.Modules.Images.Application.Commands.ConfirmUpload;
using FoodDiary.Modules.Images.Presentation.Requests;

namespace FoodDiary.Modules.Images.Presentation.Mappings;

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
