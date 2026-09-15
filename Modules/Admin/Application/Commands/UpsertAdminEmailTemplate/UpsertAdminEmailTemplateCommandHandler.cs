using FoodDiary.Mediator;
using FoodDiary.Modules.Identity.Contracts.Email.Commands.UpsertEmailTemplate;
using FoodDiary.Modules.Identity.Contracts.Admin.Models;
using FoodDiary.Modules.Admin.Application.Mappings;
using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Results;
using FoodDiary.Modules.Admin.Application.Models;

namespace FoodDiary.Modules.Admin.Application.Commands.UpsertAdminEmailTemplate;

public sealed class UpsertAdminEmailTemplateCommandHandler(
    ISender administrationService)
    : ICommandHandler<UpsertAdminEmailTemplateCommand, Result<AdminEmailTemplateModel>> {
    public async Task<Result<AdminEmailTemplateModel>> Handle(
        UpsertAdminEmailTemplateCommand command,
        CancellationToken cancellationToken) {
        Result<EmailTemplateReadModel> templateResult = await administrationService.Send(new UpsertEmailTemplateCommand(Key: command.Key, Locale: command.Locale, Subject: command.Subject, HtmlBody: command.HtmlBody, TextBody: command.TextBody, IsActive: command.IsActive), cancellationToken).ConfigureAwait(false);

        return templateResult.IsSuccess
            ? Result.Success(templateResult.Value.ToAdminModel())
            : Result.Failure<AdminEmailTemplateModel>(templateResult.Error);
    }

}
