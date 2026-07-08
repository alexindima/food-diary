using FoodDiary.Results;

namespace FoodDiary.Application.Abstractions.Images.Common;

public static class ImageErrors {
    public static Error InvalidData(string message) => new(
        "Image.InvalidData",
        message,
        Kind: ErrorKind.Validation);

    public static Error NotFound(Guid id) => new(
        "Image.NotFound",
        $"Image asset with ID {id} was not found.",
        Kind: ErrorKind.NotFound);

    public static Error Forbidden() => new(
        "Image.Forbidden",
        "Image asset does not belong to the current user.",
        Kind: ErrorKind.Forbidden);

    public static Error InUse() => new(
        "Image.InUse",
        "Image asset is already in use.",
        Kind: ErrorKind.Conflict);

    public static Error StorageError() => new(
        "Image.StorageError",
        "Failed to remove image from storage.",
        Kind: ErrorKind.ExternalFailure);
}
