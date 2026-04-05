using FoodDiary.Domain.Common;
using FoodDiary.Domain.Entities.Users;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Domain.Entities.Content;

public sealed class UserLessonProgress : Entity<UserLessonProgressId> {
    public UserId UserId { get; private set; }
    public NutritionLessonId LessonId { get; private set; }
    public DateTime ReadAtUtc { get; private set; }

    public User User { get; private set; } = null!;
    public NutritionLesson Lesson { get; private set; } = null!;

    private UserLessonProgress() {
    }

    public static UserLessonProgress Create(UserId userId, NutritionLessonId lessonId, DateTime readAtUtc) {
        if (userId == UserId.Empty) {
            throw new ArgumentException("UserId is required.", nameof(userId));
        }

        var progress = new UserLessonProgress {
            Id = UserLessonProgressId.New(),
            UserId = userId,
            LessonId = lessonId,
            ReadAtUtc = readAtUtc.Kind == DateTimeKind.Unspecified
                ? DateTime.SpecifyKind(readAtUtc, DateTimeKind.Utc)
                : readAtUtc.ToUniversalTime(),
        };
        progress.SetCreated();
        return progress;
    }
}
