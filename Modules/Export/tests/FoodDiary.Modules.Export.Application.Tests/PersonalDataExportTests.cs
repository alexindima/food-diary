using FoodDiary.Modules.Export.Application.Models;
using FoodDiary.Modules.Export.Application.Queries.ExportPersonalData;
using FoodDiary.Modules.Users.Contracts.Common;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Results;

namespace FoodDiary.Modules.Export.Application.Tests;

[ExcludeFromCodeCoverage]
public sealed class PersonalDataExportTests {
    [Fact]
    public async Task Export_ReturnsTheArchiveWithDownloadMetadataAndForwardsCancellation() {
        var id = UserId.New();
        using var cancellation = new CancellationTokenSource();
        ICurrentUserAccessService access = Substitute.For<ICurrentUserAccessService>();
        IPersonalDataArchiveReader reader = Substitute.For<IPersonalDataArchiveReader>();
        byte[] archive = [80, 75, 3, 4];
        reader.ReadArchiveAsync(id, cancellation.Token).Returns(archive);

        Result<FileExportResult> result = await new ExportPersonalDataQueryHandler(access, reader)
            .Handle(new ExportPersonalDataQuery(id.Value), cancellation.Token);

        ResultAssert.Success(result);
        Assert.Multiple(
            () => Assert.Same(archive, result.Value.Content),
            () => Assert.Equal("application/zip", result.Value.ContentType),
            () => Assert.Equal("food-diary-personal-data.zip", result.Value.FileName));
        await access.Received(1).EnsureCanAccessAsync(id, cancellation.Token);
    }

    [Fact]
    public async Task Export_DeniedAccess_DoesNotReadTheArchive() {
        var id = UserId.New();
        var denied = new Error("Privacy.Denied", "Denied", ErrorKind.Forbidden);
        ICurrentUserAccessService access = Substitute.For<ICurrentUserAccessService>();
        access.EnsureCanAccessAsync(id, Arg.Any<CancellationToken>()).Returns(denied);
        IPersonalDataArchiveReader reader = Substitute.For<IPersonalDataArchiveReader>();

        Result<FileExportResult> result = await new ExportPersonalDataQueryHandler(access, reader)
            .Handle(new ExportPersonalDataQuery(id.Value), CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal(denied, result.Error);
        await reader.DidNotReceiveWithAnyArgs().ReadArchiveAsync(default, default);
    }

    [Fact]
    public async Task Export_AccountExceedsTheBudget_ReturnsAnActionableValidationError() {
        IPersonalDataArchiveReader reader = Substitute.For<IPersonalDataArchiveReader>();
        reader.ReadArchiveAsync(Arg.Any<UserId>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException<byte[]>(new PersonalDataArchiveLimitException()));

        Result<FileExportResult> result = await new ExportPersonalDataQueryHandler(Substitute.For<ICurrentUserAccessService>(), reader)
            .Handle(new ExportPersonalDataQuery(Guid.NewGuid()), CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Multiple(
            () => Assert.Equal("Privacy.ExportLimit", result.Error.Code),
            () => Assert.Equal(ErrorKind.Validation, result.Error.Kind));
    }

    [Fact]
    public async Task Export_CancellationPropagatesWithoutBecomingAnExportLimit() {
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();
        IPersonalDataArchiveReader reader = Substitute.For<IPersonalDataArchiveReader>();
        reader.ReadArchiveAsync(Arg.Any<UserId>(), cancellation.Token).Returns(Task.FromCanceled<byte[]>(cancellation.Token));

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => new ExportPersonalDataQueryHandler(Substitute.For<ICurrentUserAccessService>(), reader)
            .Handle(new ExportPersonalDataQuery(Guid.NewGuid()), cancellation.Token));
    }
}
