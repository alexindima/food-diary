namespace FoodDiary.MailInbox.IntegrationTests.TestInfrastructure;

[CollectionDefinition("mailinbox-postgres")]
[ExcludeFromCodeCoverage]
public sealed class MailInboxPostgresCollection : ICollectionFixture<MailInboxPostgresFixture>;
