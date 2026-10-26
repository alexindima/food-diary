namespace FoodDiary.Web.Api.IntegrationTests.TestInfrastructure;

[ExcludeFromCodeCoverage]
internal sealed record CookieIdentity(Guid Id, string Email, string Cookie);
