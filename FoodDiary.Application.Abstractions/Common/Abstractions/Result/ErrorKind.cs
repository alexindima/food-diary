namespace FoodDiary.Application.Abstractions.Common.Abstractions.Result;

public enum ErrorKind {
    Validation,
    Unauthorized,
    Forbidden,
    NotFound,
    Conflict,
    RateLimited,
    ExternalFailure,
    Internal,
}
