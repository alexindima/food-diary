using FoodDiary.Application.Abstractions.Lessons.Models;

namespace FoodDiary.Application.Lessons.Common;

public interface ILessonAdministrationReadService {
    Task<IReadOnlyList<LessonAdminReadModel>> GetLessonsAsync(CancellationToken cancellationToken);
}
