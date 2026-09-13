using FluentValidation;

namespace FoodDiary.Modules.Admin.Application.Queries.GetAdminMailInboxMessageDetails;

public sealed class GetAdminMailInboxMessageDetailsQueryValidator
    : AbstractValidator<GetAdminMailInboxMessageDetailsQuery> {
    public GetAdminMailInboxMessageDetailsQueryValidator() {
        RuleFor(static query => query.Id)
            .NotEmpty();
    }
}
