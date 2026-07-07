using FoodDiary.Application.Abstractions.Images.Common;

namespace FoodDiary.Application.Abstractions.Common.Abstractions.Results;

public static partial class Errors {
    public static class Image {
        public static Error InvalidData(string message) => ImageErrors.InvalidData(message);

        public static Error NotFound(Guid id) => ImageErrors.NotFound(id);

        public static Error Forbidden() => ImageErrors.Forbidden();

        public static Error InUse() => ImageErrors.InUse();

        public static Error StorageError() => ImageErrors.StorageError();
    }
}
