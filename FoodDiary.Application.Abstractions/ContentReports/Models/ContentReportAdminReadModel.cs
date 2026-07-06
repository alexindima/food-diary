using System.Diagnostics.CodeAnalysis;

namespace FoodDiary.Application.Abstractions.ContentReports.Models;

[ExcludeFromCodeCoverage]
public sealed record ContentReportAdminReadModel(
    Guid Id,
    Guid UserId,
    string TargetType,
    Guid TargetId,
    string Reason,
    string Status,
    string? AdminNote,
    DateTime CreatedOnUtc,
    DateTime? ReviewedAtUtc);
