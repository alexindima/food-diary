using FoodDiary.Application.Images.Commands.GetUploadUrl;
using FoodDiary.Presentation.Api.Features.Images.Mappings;
using FoodDiary.Presentation.Api.Features.Images.Requests;

namespace FoodDiary.Presentation.Api.Tests;

public sealed class ImageHttpMappingsTests {
    [Fact]
    public void ToDeleteCommand_MapsUserIdAndAssetId() {
        var userId = Guid.NewGuid();
        var assetId = Guid.NewGuid();

        var command = assetId.ToDeleteCommand(userId);

        Assert.Equal(userId, command.UserId);
        Assert.Equal(assetId, command.AssetId);
    }

    [Fact]
    public void GetImageUploadUrlRequest_ToCommand_MapsAllFields() {
        var userId = Guid.NewGuid();
        var request = new GetImageUploadUrlHttpRequest("photo.jpg", "image/jpeg", 1024000);

        var command = request.ToCommand(userId);

        Assert.Equal(userId, command.UserId);
        Assert.Equal("photo.jpg", command.FileName);
        Assert.Equal("image/jpeg", command.ContentType);
        Assert.Equal(1024000, command.FileSizeBytes);
    }

    [Fact]
    public void GetImageUploadUrlResult_ToHttpResponse_MapsAllFields() {
        var assetId = Guid.NewGuid();
        var expiresAt = DateTime.UtcNow.AddMinutes(15);
        var result = new GetImageUploadUrlResult(
            "https://s3.example.com/upload", "https://cdn.example.com/file.jpg",
            "users/123/images/file.jpg", expiresAt, assetId);

        var response = result.ToHttpResponse();

        Assert.Equal("https://s3.example.com/upload", response.UploadUrl);
        Assert.Equal("https://cdn.example.com/file.jpg", response.FileUrl);
        Assert.Equal("users/123/images/file.jpg", response.ObjectKey);
        Assert.Equal(expiresAt, response.ExpiresAtUtc);
        Assert.Equal(assetId, response.AssetId);
    }
}
