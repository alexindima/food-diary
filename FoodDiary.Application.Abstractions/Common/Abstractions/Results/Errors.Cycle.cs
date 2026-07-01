namespace FoodDiary.Application.Abstractions.Common.Abstractions.Results;

public static partial class Errors {
    public static class Cycle {
        public static Error NotFound(Guid id) => new(
            "Cycle.NotFound",
            $"Cycle with ID {id} was not found.",
            Kind: ErrorKind.NotFound);
    }
}
