using System.Runtime.InteropServices;

namespace FoodDiary.Domain.ValueObjects;

[StructLayout(LayoutKind.Auto)]
public readonly record struct HealthAreaScore {
    public int Score { get; }
    public HealthAreaGrade Grade { get; }

    public HealthAreaScore(int score, HealthAreaGrade grade) {
        if (score is < 0 or > 100) {
            throw new ArgumentOutOfRangeException(nameof(score), "Score must be between 0 and 100.");
        }

        HealthAreaGrade expectedGrade = score switch {
            >= 75 => HealthAreaGrade.Excellent,
            >= 50 => HealthAreaGrade.Good,
            >= 25 => HealthAreaGrade.Fair,
            > 0 => HealthAreaGrade.Low,
            _ => HealthAreaGrade.Unknown,
        };
        if (grade != expectedGrade) {
            throw new ArgumentException("Grade must match the score range.", nameof(grade));
        }

        Score = score;
        Grade = grade;
    }
}
