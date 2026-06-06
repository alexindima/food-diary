namespace FoodDiary.MailRelay.Application.Common.Result;

public enum ErrorKind {
    Validation = 0,
    Unauthorized = 1,
    NotFound = 2,
    Conflict = 3,
    ExternalFailure = 4,
    Internal = 5,
}
