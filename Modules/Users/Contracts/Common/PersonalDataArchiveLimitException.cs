namespace FoodDiary.Modules.Users.Contracts.Common;

public sealed class PersonalDataArchiveLimitException() : Exception("The self-service archive limit was exceeded. Contact support for a complete export.");
