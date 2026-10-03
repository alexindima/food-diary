namespace FoodDiary.Persistence.Runtime.Persistence.Shared;

public sealed class CommitOutcomeUnknownException(Exception innerException)
    : Exception("The database commit outcome could not be verified. The operation was not replayed.", innerException);
