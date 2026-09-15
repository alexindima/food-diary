using FoodDiary.Mediator;
using FoodDiary.Presentation.Api.Features.Version;
using FoodDiary.Presentation.Api.Features.Version.Responses;
using Microsoft.AspNetCore.Mvc;

namespace FoodDiary.Presentation.Api.Tests;

[ExcludeFromCodeCoverage]
public sealed class VersionControllerTests {
    [Fact]
    public void GetVersion_ReturnsDeploymentMetadata() {
        ISender sender = Substitute.For<ISender>();
        IApiVersionInfo versionInfo = Substitute.For<IApiVersionInfo>();
        DateTimeOffset startedAt = DateTimeOffset.UtcNow.AddMinutes(-5);
        versionInfo.CommitSha.Returns("commit-sha");
        versionInfo.ImageTag.Returns("image-tag");
        versionInfo.Environment.Returns("Test");
        versionInfo.ApplicationVersion.Returns("1.2.3");
        versionInfo.StartedAtUtc.Returns(startedAt);
        VersionController controller = new(sender, versionInfo);

        IActionResult result = controller.GetVersion();

        OkObjectResult ok = Assert.IsType<OkObjectResult>(result);
        ApiVersionHttpResponse response = Assert.IsType<ApiVersionHttpResponse>(ok.Value);
        Assert.Multiple(
            () => Assert.Equal("commit-sha", response.CommitSha),
            () => Assert.Equal("image-tag", response.ImageTag),
            () => Assert.Equal("Test", response.Environment),
            () => Assert.Equal("1.2.3", response.ApplicationVersion),
            () => Assert.Equal(startedAt, response.StartedAtUtc));
    }
}
