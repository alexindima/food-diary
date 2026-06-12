namespace FoodDiary.MailRelay.Tests.TestInfrastructure;

[CollectionDefinition("mailrelay-environment")]
[ExcludeFromCodeCoverage]
public sealed class MailRelayEnvironmentCollection : ICollectionFixture<MailRelayEnvironmentFixture>;
