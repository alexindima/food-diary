using FoodDiary.Application.Abstractions.Common.Abstractions.Results;

namespace FoodDiary.Application.Abstractions.ContentReports.Common;

public static class ContentReportErrors {
    public static Error NotFound(Guid id) => new(
        "ContentReport.NotFound",
        $"Content report with ID {id} was not found.",
        Kind: ErrorKind.NotFound);

    public static Error AlreadyReported => new(
        "ContentReport.AlreadyReported",
        "You have already reported this content.",
        Kind: ErrorKind.Conflict);
}
