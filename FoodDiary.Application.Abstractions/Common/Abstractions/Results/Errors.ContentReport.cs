using FoodDiary.Results;

using FoodDiary.Application.Abstractions.ContentReports.Common;

namespace FoodDiary.Application.Abstractions.Common.Abstractions.Results;

public static partial class Errors {
    public static class ContentReport {
        public static Error NotFound(Guid id) => ContentReportErrors.NotFound(id);

        public static Error AlreadyReported => ContentReportErrors.AlreadyReported;
    }
}
