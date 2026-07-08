using FoodDiary.Results;
using FoodDiary.Application.Export.Models;
using FoodDiary.Application.Export.Queries.ExportCycle;
using FoodDiary.Application.Export.Queries.ExportDiary;
using FoodDiary.Mediator;
using FoodDiary.Presentation.Api.Features.Export;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FoodDiary.Presentation.Api.Tests;

[ExcludeFromCodeCoverage]
public sealed class ExportControllerTests {
    [Fact]
    public async Task ExportDiary_SendsDiaryQueryAndReturnsFile() {
        byte[] content = [1, 2, 3];
        IRequest<Result<FileExportResult>>? sentRequest = null;
        ISender sender = SubstituteSender.Create(
            Result.Success(new FileExportResult(content, "text/csv", "diary.csv")),
            request => sentRequest = request);
        ExportController controller = CreateController(sender);
        var userId = Guid.NewGuid();
        DateTime dateFrom = DateTime.UtcNow.AddDays(-7);
        DateTime dateTo = DateTime.UtcNow;

        IActionResult result = await controller.ExportDiary(
            userId,
            dateFrom,
            dateTo,
            format: "pdf",
            locale: "ru",
            timeZoneOffsetMinutes: 240,
            reportOrigin: "mobile");

        FileContentResult file = Assert.IsType<FileContentResult>(result);
        Assert.Equal(content, file.FileContents);
        Assert.Equal("text/csv", file.ContentType);
        Assert.Equal("diary.csv", file.FileDownloadName);
        ExportDiaryQuery query = Assert.IsType<ExportDiaryQuery>(sentRequest);
        Assert.Equal(userId, query.UserId);
        Assert.Equal(dateFrom, query.DateFrom);
        Assert.Equal(dateTo, query.DateTo);
        Assert.Equal(ExportFormat.Pdf, query.Format);
        Assert.Equal("ru", query.Locale);
        Assert.Equal(240, query.TimeZoneOffsetMinutes);
        Assert.Equal("mobile", query.ReportOrigin);
    }

    [Fact]
    public async Task ExportCycle_SendsCycleQueryAndReturnsFile() {
        byte[] content = [4, 5, 6];
        IRequest<Result<FileExportResult>>? sentRequest = null;
        ISender sender = SubstituteSender.Create(
            Result.Success(new FileExportResult(content, "application/pdf", "cycle.pdf")),
            request => sentRequest = request);
        ExportController controller = CreateController(sender);
        var userId = Guid.NewGuid();
        DateTime dateFrom = DateTime.UtcNow.AddDays(-30);
        DateTime dateTo = DateTime.UtcNow;

        IActionResult result = await controller.ExportCycle(userId, dateFrom, dateTo, timeZoneOffsetMinutes: 180);

        FileContentResult file = Assert.IsType<FileContentResult>(result);
        Assert.Equal(content, file.FileContents);
        Assert.Equal("application/pdf", file.ContentType);
        Assert.Equal("cycle.pdf", file.FileDownloadName);
        ExportCycleQuery query = Assert.IsType<ExportCycleQuery>(sentRequest);
        Assert.Equal(userId, query.UserId);
        Assert.Equal(dateFrom, query.DateFrom);
        Assert.Equal(dateTo, query.DateTo);
        Assert.Equal(180, query.TimeZoneOffsetMinutes);
    }

    private static ExportController CreateController(ISender sender) =>
        new(sender) {
            ControllerContext = new ControllerContext {
                HttpContext = new DefaultHttpContext(),
            },
        };
}
