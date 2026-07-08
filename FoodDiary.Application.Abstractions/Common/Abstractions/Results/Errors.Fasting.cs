using FoodDiary.Results;

using FoodDiary.Application.Abstractions.Fasting.Common;

namespace FoodDiary.Application.Abstractions.Common.Abstractions.Results;

public static partial class Errors {
    public static class Fasting {
        public static Error AlreadyActive => FastingErrors.AlreadyActive;

        public static Error NoActiveSession => FastingErrors.NoActiveSession;

        public static Error InvalidProtocol => FastingErrors.InvalidProtocol;

        public static Error InvalidCyclicAction(string message) => FastingErrors.InvalidCyclicAction(message);
    }
}
