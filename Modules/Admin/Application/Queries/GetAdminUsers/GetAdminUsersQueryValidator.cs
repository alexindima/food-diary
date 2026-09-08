using FluentValidation;

namespace FoodDiary.Application.Admin.Queries.GetAdminUsers;

public sealed class GetAdminUsersQueryValidator : AbstractValidator<GetAdminUsersQuery> {
    public GetAdminUsersQueryValidator() {
        RuleFor(query => query.Filter).Must(filter => filter is null ||
            ((!filter.RegisteredFrom.HasValue || !filter.RegisteredTo.HasValue || filter.RegisteredFrom <= filter.RegisteredTo) &&
            (!filter.LastLoginFrom.HasValue || !filter.LastLoginTo.HasValue || filter.LastLoginFrom <= filter.LastLoginTo)))
            .WithMessage("The start date must not follow the end date.");
    }
}
