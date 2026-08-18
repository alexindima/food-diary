using System.Runtime.InteropServices;

namespace FoodDiary.Domain.ValueObjects;

[StructLayout(LayoutKind.Auto)]
public readonly record struct HealthAreaScore(int Score, HealthAreaGrade Grade);
