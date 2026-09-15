using FoodDiary.Modules.Identity.Application.Abstractions.Admin.Common;
using FoodDiary.Modules.Identity.Contracts.Admin.Models;
using FoodDiary.Modules.Identity.Domain.Entities.Content;
using FoodDiary.Results;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects;
using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Modules.Identity.Contracts.Email.Commands.UpsertEmailTemplate;
using FoodDiary.Mediator;

namespace FoodDiary.Modules.Identity.Application.Email.Commands.UpsertEmailTemplate;

public sealed class UpsertEmailTemplateCommandHandler(IEmailTemplateWriteRepository repository) : IRequestHandler<UpsertEmailTemplateCommand, Result<EmailTemplateReadModel>> {
    public async Task<Result<EmailTemplateReadModel>> Handle(UpsertEmailTemplateCommand request, CancellationToken cancellationToken) {
        string key = request.Key.Trim().ToLowerInvariant();
        if (!LanguageCode.TryParse(request.Locale, out LanguageCode language)) {
            return Result.Failure<EmailTemplateReadModel>(
                Errors.Validation.Invalid(nameof(request.Locale), "Locale must be one of the supported codes."));
        }
        string locale = language.Value;
        string subject = request.Subject;
        string htmlBody = request.HtmlBody;
        string textBody = request.TextBody;
        bool isActive = request.IsActive;
        EmailTemplate template = await repository.UpsertAsync(
            key,
            locale,
            subject,
            htmlBody,
            textBody,
            isActive,
            cancellationToken).ConfigureAwait(false);

        return Result.Success(new EmailTemplateReadModel(
            template.Id, template.Key, template.Locale, template.Subject, template.HtmlBody,
            template.TextBody, template.IsActive, template.CreatedOnUtc, template.ModifiedOnUtc));

    }

}
