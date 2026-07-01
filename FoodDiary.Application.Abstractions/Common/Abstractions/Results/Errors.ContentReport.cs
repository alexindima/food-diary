namespace FoodDiary.Application.Abstractions.Common.Abstractions.Results;

public static partial class Errors {
    public static class ContentReport {
        public static Error NotFound(Guid id) => new(
            "ContentReport.NotFound",
            $"Content report with ID {id} was not found.",
            Kind: ErrorKind.NotFound);

        public static Error AlreadyReported => new(
            "ContentReport.AlreadyReported",
            "You have already reported this content.",
            Kind: ErrorKind.Conflict);
    }
}
