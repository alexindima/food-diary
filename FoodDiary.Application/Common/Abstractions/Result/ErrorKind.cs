namespace FoodDiary.Application.Common.Abstractions.Result;

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
