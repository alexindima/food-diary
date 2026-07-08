using FoodDiary.Results;

using FoodDiary.Application.Abstractions.Consumptions.Common;

namespace FoodDiary.Application.Abstractions.Common.Abstractions.Results;

public static partial class Errors {
    public static class Consumption {
        public static Error NotFound(Guid id) => ConsumptionErrors.NotFound(id);

        public static Error InvalidData(string message) => ConsumptionErrors.InvalidData(message);
    }
}
