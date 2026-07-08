using FoodDiary.Results;

using FoodDiary.Application.Abstractions.Users.Common;

namespace FoodDiary.Application.Abstractions.Common.Abstractions.Results;

public static partial class Errors {
    public static class User {
        public static Error NotFound(Guid id) => UserErrors.NotFound(id);

        public static Error InvalidPassword => UserErrors.InvalidPassword;

        public static Error PasswordNotSet => UserErrors.PasswordNotSet;

        public static Error PasswordAlreadySet => UserErrors.PasswordAlreadySet;

        public static Error NotFound() => UserErrors.NotFound();

        public static Error InvalidCredentials => UserErrors.InvalidCredentials;

        public static Error EmailAlreadyExists => UserErrors.EmailAlreadyExists;
    }
}
