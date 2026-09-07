namespace FoodDiary.MailRelay.IntegrationTests.TestInfrastructure;

[CollectionDefinition("mailrelay-environment")]
[ExcludeFromCodeCoverage]
public sealed class MailRelayEnvironmentCollection : ICollectionFixture<MailRelayEnvironmentFixture>;
