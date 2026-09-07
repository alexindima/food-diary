using FoodDiary.MailInbox.Application.Health;
using FoodDiary.MailInbox.Presentation.Features.Health.Responses;

namespace FoodDiary.MailInbox.Presentation.Features.Health.Mappings;

public static class MailInboxHealthHttpMappings {
    public static MailInboxHealthHttpResponse ToHealthHttpResponse() {
        return new MailInboxHealthHttpResponse("ok");
    }

    public static CheckMailInboxReadinessQuery ToReadinessQuery() {
        return new CheckMailInboxReadinessQuery();
    }

    public static MailInboxHealthHttpResponse ToReadyHttpResponse() {
        return new MailInboxHealthHttpResponse("ready");
    }
}
