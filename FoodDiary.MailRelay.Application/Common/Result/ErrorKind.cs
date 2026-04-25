namespace FoodDiary.MailRelay.Application.Common.Result;

public enum ErrorKind {
    Validation,
    Unauthorized,
    NotFound,
    Conflict,
    ExternalFailure,
    Internal,
}
