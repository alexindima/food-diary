using System.Runtime.InteropServices;
using FoodDiary.Domain.Enums;

namespace FoodDiary.Domain.ValueObjects;

[StructLayout(LayoutKind.Auto)]
public readonly record struct UserActivityUpdate(
    ActivityLevel? ActivityLevel = null,
    int? StepGoal = null,
    double? HydrationGoal = null);
