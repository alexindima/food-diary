using FoodDiary.Modules.Users.Application.Commands.RecordUserActivity;

namespace FoodDiary.Modules.Users.Presentation.Mappings;

public static class UserActivityHttpMappings {
    extension(Guid userId) {
        public RecordUserActivityCommand ToActivityCommand() => new(userId);
    }
}
