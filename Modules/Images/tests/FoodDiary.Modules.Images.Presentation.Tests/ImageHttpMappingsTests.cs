using FoodDiary.Modules.Images.Presentation.Mappings;
using FoodDiary.Modules.Images.Application.Commands.DeleteImageAsset;
using FoodDiary.Modules.Images.Application.Commands.GetUploadUrl;

using FoodDiary.Modules.Images.Presentation.Requests;
using FoodDiary.Modules.Images.Presentation.Responses;

namespace FoodDiary.Modules.Images.Presentation.Tests;

[ExcludeFromCodeCoverage]
public sealed class ImageHttpMappingsTests {
    [Fact]
    public void ToDeleteCommand_MapsUserIdAndAssetId() {
        var userId = Guid.NewGuid();
        var assetId = Guid.NewGuid();

        DeleteImageAssetCommand command = assetId.ToDeleteCommand(userId);

        Assert.Equal(userId, command.UserId);
        Assert.Equal(assetId, command.AssetId);
    }

    [Fact]
    public void GetImageUploadUrlRequest_ToCommand_MapsAllFields() {
        var userId = Guid.NewGuid();
        var request = new GetImageUploadUrlHttpRequest("photo.jpg", "image/jpeg", 1024000);

        GetImageUploadUrlCommand command = request.ToCommand(userId);

        Assert.Multiple(
            () => Assert.Equal(userId, command.UserId),
            () => Assert.Equal("photo.jpg", command.FileName),
            () => Assert.Equal("image/jpeg", command.ContentType),
            () => Assert.Equal(1024000, command.FileSizeBytes));
    }

    [Fact]
    public void GetImageUploadUrlResult_ToHttpResponse_MapsAllFields() {
        var assetId = Guid.NewGuid();
        DateTime expiresAt = DateTime.UtcNow.AddMinutes(15);
        var result = new GetImageUploadUrlResult(
            "https://s3.example.com/upload", "https://cdn.example.com/file.jpg",
            expiresAt, assetId);

        GetImageUploadUrlHttpResponse response = result.ToHttpResponse();

        Assert.Multiple(
            () => Assert.Equal("https://s3.example.com/upload", response.UploadUrl),
            () => Assert.Equal("https://cdn.example.com/file.jpg", response.FileUrl),
            () => Assert.Equal(expiresAt, response.ExpiresAtUtc),
            () => Assert.Equal(assetId, response.AssetId));
    }
}
