using FoodDiary.Modules.Lessons.Contracts.Models;

namespace FoodDiary.Modules.Lessons.Contracts.Common;

public interface ILessonAdministrationReadService {
    Task<IReadOnlyList<LessonAdminReadModel>> GetLessonsAsync(CancellationToken cancellationToken);
}
