using FoodDiary.MailInbox.Application.Messages.Models;

namespace FoodDiary.MailInbox.Infrastructure.Services;

public interface IMailInboxDmarcReportParser {
    DmarcReportPreview? TryParse(string rawMime, CancellationToken cancellationToken = default);
}
