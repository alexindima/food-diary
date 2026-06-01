using FoodDiary.Application.Abstractions.Common.Abstractions.Result;
using FoodDiary.Mediator;

namespace FoodDiary.Application.Images.Commands.GetUploadUrl;

public sealed record GetImageUploadUrlCommand(
    Guid UserId,
    string FileName,
    string ContentType,
    long FileSizeBytes) : IRequest<Result<GetImageUploadUrlResult>>;
