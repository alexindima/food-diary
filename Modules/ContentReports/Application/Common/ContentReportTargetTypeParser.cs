using FoodDiary.Modules.ContentReports.Domain.Contracts.Enums;
using FoodDiary.Application.Abstractions.Common.Validation;
using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Results;

namespace FoodDiary.Modules.ContentReports.Application.Common;

internal static class ContentReportTargetTypeParser {
    public static Result<ReportTargetType> ParseRequired(string? value, string fieldName, string message) =>
        SharedEnumValueParser.TryParse(value, out ReportTargetType parsed)
            ? Result.Success(parsed)
            : Result.Failure<ReportTargetType>(Errors.Validation.Invalid(fieldName, message));
}
