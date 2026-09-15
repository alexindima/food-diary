using FoodDiary.Modules.ContentReports.Application.Models;
using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Results;

namespace FoodDiary.Modules.ContentReports.Application.Commands.CreateContentReport;

public record CreateContentReportCommand(
    Guid? UserId,
    string TargetType,
    Guid TargetId,
    string Reason) : ICommand<Result<ContentReportModel>>, IUserRequest;
