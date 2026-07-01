namespace FoodDiary.Application.Abstractions.Common.Abstractions.Results;

public static partial class Errors {
    public static class Fasting {
        public static Error AlreadyActive => new(
            "Fasting.AlreadyActive",
            "A fasting session is already active.",
            Kind: ErrorKind.Conflict);

        public static Error NoActiveSession => new(
            "Fasting.NoActiveSession",
            "No active fasting session found.",
            Kind: ErrorKind.NotFound);

        public static Error InvalidProtocol => new(
            "Fasting.InvalidProtocol",
            "Invalid fasting protocol.",
            Kind: ErrorKind.Validation);

        public static Error InvalidCyclicAction(string message) => new(
            "Fasting.InvalidCyclicAction",
            message,
            Kind: ErrorKind.Validation);
    }
}
