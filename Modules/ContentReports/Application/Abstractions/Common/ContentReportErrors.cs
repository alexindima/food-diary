using FoodDiary.Results;

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

    public static Error TargetNotFound => new(
        "ContentReport.TargetNotFound",
        "The reported content does not exist.",
        Kind: ErrorKind.NotFound);

    public static Error AlreadyResolved => new(
        "ContentReport.AlreadyResolved",
        "The content report has already been resolved.",
        Kind: ErrorKind.Conflict);
}
