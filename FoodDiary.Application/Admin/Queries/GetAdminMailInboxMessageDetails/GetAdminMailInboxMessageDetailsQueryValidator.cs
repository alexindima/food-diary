using FluentValidation;

namespace FoodDiary.Application.Admin.Queries.GetAdminMailInboxMessageDetails;

public sealed class GetAdminMailInboxMessageDetailsQueryValidator
    : AbstractValidator<GetAdminMailInboxMessageDetailsQuery> {
    public GetAdminMailInboxMessageDetailsQueryValidator() {
        RuleFor(static query => query.Id)
            .NotEmpty();
    }
}
