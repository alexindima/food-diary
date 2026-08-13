using FluentValidation;

namespace FoodDiary.Application.Admin.Queries.GetAdminMailInboxMessages;

public sealed class GetAdminMailInboxMessagesQueryValidator : AbstractValidator<GetAdminMailInboxMessagesQuery> {
    public GetAdminMailInboxMessagesQueryValidator() {
        RuleFor(static query => query.Limit)
            .InclusiveBetween(1, 200);
    }
}
