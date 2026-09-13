using FoodDiary.Modules.Admin.Application.Abstractions.Models;
using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Results;

namespace FoodDiary.Application.Admin.Queries.GetAdminBugReports;

public sealed record GetAdminBugReportsQuery(AdminBugReportFilter Filter) : IQuery<Result<AdminBugReportPage>>;
