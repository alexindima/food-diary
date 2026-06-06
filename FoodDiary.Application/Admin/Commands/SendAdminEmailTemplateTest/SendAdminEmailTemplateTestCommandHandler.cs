using System.Net.Mail;
using System.Net.Mime;
using System.Text;
using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Email.Common;
using FoodDiary.Application.Abstractions.Email.Common;

namespace FoodDiary.Application.Admin.Commands.SendAdminEmailTemplateTest;

public sealed class SendAdminEmailTemplateTestCommandHandler(
    EmailOptions options,
    IEmailTransport emailTransport)
    : ICommandHandler<SendAdminEmailTemplateTestCommand, Result> {
    public async Task<Result> Handle(SendAdminEmailTemplateTestCommand command, CancellationToken cancellationToken) {
        string brand = string.IsNullOrWhiteSpace(options.FromName) ? "FoodDiary" : options.FromName;
        string link = GetSampleLink(command.Key);
        const string clientName = "Alex Johnson";

        string subject = ApplyTemplateTokens(command.Subject, link, brand, clientName);
        string htmlBody = ApplyTemplateTokens(command.HtmlBody, link, brand, clientName);
        string textBody = ApplyTemplateTokens(command.TextBody, link, brand, clientName);

        using var message = new MailMessage();
        message.From = new MailAddress(options.FromAddress, options.FromName);
        message.Subject = subject;
        message.Body = htmlBody;
        message.IsBodyHtml = true;
        message.BodyEncoding = Encoding.UTF8;
        message.SubjectEncoding = Encoding.UTF8;
        message.To.Add(new MailAddress(command.ToEmail));
        message.AlternateViews.Add(AlternateView.CreateAlternateViewFromString(textBody, Encoding.UTF8, MediaTypeNames.Text.Plain));
        message.AlternateViews.Add(AlternateView.CreateAlternateViewFromString(htmlBody, Encoding.UTF8, MediaTypeNames.Text.Html));

        await emailTransport.SendAsync(message, cancellationToken).ConfigureAwait(false);
        ApplicationEmailTelemetry.RecordEmailDispatch($"admin_template_test:{NormalizeKey(command.Key)}", "test", "success");

        return Result.Success();
    }

    private static string ApplyTemplateTokens(string value, string link, string brand, string clientName) {
        return value
            .Replace("{{link}}", link, StringComparison.OrdinalIgnoreCase)
            .Replace("{{brand}}", brand, StringComparison.OrdinalIgnoreCase)
            .Replace("{{clientName}}", clientName, StringComparison.OrdinalIgnoreCase);
    }

    private static string GetSampleLink(string key) {
        return string.Equals(NormalizeKey(key), "dietologist_invitation", StringComparison.Ordinal)
            ? "https://fooddiary.club/dietologist/accept?invitationId=demo&token=demo"
            : "https://fooddiary.club/verify-email?userId=demo&token=demo";
    }

    private static string NormalizeKey(string value) {
        return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim().ToLowerInvariant();
    }
}
