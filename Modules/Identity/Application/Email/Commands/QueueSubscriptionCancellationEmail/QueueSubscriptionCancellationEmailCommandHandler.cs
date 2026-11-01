using FoodDiary.Email.Contracts.Email.Common;
using FoodDiary.Mediator;
using FoodDiary.Modules.Identity.Contracts.Authentication.Common;
using FoodDiary.Modules.Identity.Contracts.Email.Commands.QueueSubscriptionCancellationEmail;
using FoodDiary.Results;

namespace FoodDiary.Modules.Identity.Application.Email.Commands.QueueSubscriptionCancellationEmail;

public sealed class QueueSubscriptionCancellationEmailCommandHandler(
    EmailOptions options,
    IEmailTemplateProvider templates,
    IEmailOutbox outbox) : IRequestHandler<QueueSubscriptionCancellationEmailCommand, Result> {
    public async Task<Result> Handle(QueueSubscriptionCancellationEmailCommand request, CancellationToken cancellationToken) {
        string locale = string.Equals(request.Language, "ru", StringComparison.OrdinalIgnoreCase) ? "ru" : "en";
        const string templateKey = "subscription_cancellation";
        EmailTemplateContent? template = await templates.GetActiveTemplateAsync(templateKey, locale, cancellationToken);
        EmailTemplateContent content = SubscriptionCancellationEmailContent.Render(request, options, locale, template);
        await outbox.EnqueueAsync(new EmailMessage(options.FromAddress, options.FromName, [request.ToEmail],
            content.Subject, content.HtmlBody, content.TextBody, Purpose: templateKey,
            ReplyTo: SubscriptionCancellationEmailContent.FeedbackAddress, AutoSubmitted: true, CorrelationId: request.CorrelationId), cancellationToken);
        ApplicationEmailTelemetry.RecordEmailDispatch(templateKey, locale, "queued");
        return Result.Success();
    }
}
