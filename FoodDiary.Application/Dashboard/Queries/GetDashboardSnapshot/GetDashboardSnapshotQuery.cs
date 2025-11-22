using System;
using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Abstractions.Result;
using FoodDiary.Contracts.Dashboard;
using FoodDiary.Domain.ValueObjects;

namespace FoodDiary.Application.Dashboard.Queries.GetDashboardSnapshot;

public record GetDashboardSnapshotQuery(
    UserId? UserId,
    DateTime Date,
    int Page,
    int PageSize) : IQuery<Result<DashboardSnapshotResponse>>;
