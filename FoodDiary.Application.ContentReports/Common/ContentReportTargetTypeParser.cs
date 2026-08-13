using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Results;
using FoodDiary.Domain.Enums;

namespace FoodDiary.Application.ContentReports.Common;

internal static class ContentReportTargetTypeParser {
    public static Result<ReportTargetType> ParseRequired(string? value, string fieldName, string message) =>
        Enum.TryParse(value, ignoreCase: true, out ReportTargetType parsed)
            ? Result.Success(parsed)
            : Result.Failure<ReportTargetType>(Errors.Validation.Invalid(fieldName, message));
}
