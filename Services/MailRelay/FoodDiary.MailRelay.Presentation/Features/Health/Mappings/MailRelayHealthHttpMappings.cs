using FoodDiary.MailRelay.Presentation.Features.Health.Responses;

namespace FoodDiary.MailRelay.Presentation.Features.Health.Mappings;

public static class MailRelayHealthHttpMappings {
    public static CheckMailRelayReadinessQuery ToReadinessQuery() => new();

    public static MailRelayHealthHttpResponse ToHealthHttpResponse() => new("ok");

    public static MailRelayHealthHttpResponse ToReadyHttpResponse() => new("ready");
}
