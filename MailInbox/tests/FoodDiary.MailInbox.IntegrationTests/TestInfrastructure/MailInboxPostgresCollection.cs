namespace FoodDiary.MailInbox.Tests.TestInfrastructure;

[CollectionDefinition("mailinbox-postgres")]
[ExcludeFromCodeCoverage]
public sealed class MailInboxPostgresCollection : ICollectionFixture<MailInboxPostgresFixture>;
