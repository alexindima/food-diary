namespace FoodDiary.Results;

public enum ErrorKind {
    Validation = 0,
    Unauthorized = 1,
    Forbidden = 2,
    NotFound = 3,
    Conflict = 4,
    RateLimited = 5,
    ExternalFailure = 6,
    Internal = 7,
}
