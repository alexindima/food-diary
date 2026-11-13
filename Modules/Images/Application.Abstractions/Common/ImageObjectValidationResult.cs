namespace FoodDiary.Modules.Images.Application.Abstractions.Common;

public union ImageObjectValidationResult(ImageObjectValidationResult.Valid, ImageObjectValidationResult.Invalid) {
    public static ImageObjectValidationResult Accepted { get; } = new Valid();

    public bool IsValid => this is Valid;
    public string? ErrorCode => this is Invalid invalid ? invalid.Code : null;
    public string? Message => this is Invalid invalid ? invalid.Reason : null;

    public sealed record Valid;
    public sealed record Invalid(string? Code = null, string? Reason = null);
}
