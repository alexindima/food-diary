using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Abstractions.Result;
using FoodDiary.Application.ContentReports.Models;

namespace FoodDiary.Application.ContentReports.Commands.CreateContentReport;

public record CreateContentReportCommand(
    Guid? UserId,
    string TargetType,
    Guid TargetId,
    string Reason) : ICommand<Result<ContentReportModel>>, IUserRequest;
