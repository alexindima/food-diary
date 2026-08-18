namespace FoodDiary.Results;

/// <summary>
/// Classifies an error independently from any specific transport protocol.
/// </summary>
public enum ErrorKind {
    /// <summary>The input did not satisfy validation rules.</summary>
    Validation = 0,

    /// <summary>Authentication is required or failed.</summary>
    Unauthorized = 1,

    /// <summary>The authenticated caller is not allowed to perform the operation.</summary>
    Forbidden = 2,

    /// <summary>The requested resource was not found.</summary>
    NotFound = 3,

    /// <summary>The operation conflicts with the current state.</summary>
    Conflict = 4,

    /// <summary>The caller exceeded an allowed rate or quota.</summary>
    RateLimited = 5,

    /// <summary>An external dependency failed.</summary>
    ExternalFailure = 6,

    /// <summary>An unexpected internal failure occurred.</summary>
    Internal = 7,
}
