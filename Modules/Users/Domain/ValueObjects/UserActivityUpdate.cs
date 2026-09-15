using System.Runtime.InteropServices;
using FoodDiary.Modules.Users.Domain.Contracts.Enums;

namespace FoodDiary.Modules.Users.Domain.ValueObjects;

[StructLayout(LayoutKind.Auto)]
public readonly record struct UserActivityUpdate(
    ActivityLevel? ActivityLevel = null,
    int? StepGoal = null,
    double? HydrationGoal = null);
