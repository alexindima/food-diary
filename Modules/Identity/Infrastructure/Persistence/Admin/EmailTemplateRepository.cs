using FoodDiary.Modules.Identity.Application.Abstractions.Admin.Common;
using FoodDiary.Modules.Identity.Contracts.Admin.Models;
using FoodDiary.Modules.Identity.Domain.Entities.Content;
using Microsoft.EntityFrameworkCore;

namespace FoodDiary.Modules.Identity.Infrastructure.Persistence.Admin;

public sealed class EmailTemplateRepository(DbSet<EmailTemplate> emailTemplates, Func<CancellationToken, Task>? synchronizeTransactionAsync = null) : IEmailTemplateRepository {
    public async Task<IReadOnlyList<EmailTemplateRevisionReadModel>> GetRevisionsAsync(string key, string locale, CancellationToken cancellationToken) {
        if (synchronizeTransactionAsync is not null) {
            await synchronizeTransactionAsync(cancellationToken).ConfigureAwait(false);
        }
        return await emailTemplates.AsNoTracking().Where(template => template.Key == key && template.Locale == locale)
            .SelectMany(template => template.Revisions).OrderByDescending(revision => revision.ArchivedOnUtc).ThenByDescending(revision => revision.Id).Take(50)
            .Select(revision => new EmailTemplateRevisionReadModel(revision.Id, revision.Subject, revision.HtmlBody,
                revision.TextBody, revision.IsActive, revision.SavedOnUtc, revision.ArchivedOnUtc))
            .ToListAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<EmailTemplate>> GetAllAsync(CancellationToken cancellationToken = default) {
        if (synchronizeTransactionAsync is not null) {
            await synchronizeTransactionAsync(cancellationToken).ConfigureAwait(false);
        }
        return await emailTemplates
            .AsNoTracking()
            .OrderBy(t => t.Key)
            .ThenBy(t => t.Locale)
            .ToListAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<EmailTemplateReadModel>> GetAllReadModelsAsync(CancellationToken cancellationToken = default) {
        if (synchronizeTransactionAsync is not null) {
            await synchronizeTransactionAsync(cancellationToken).ConfigureAwait(false);
        }
        return await emailTemplates
            .AsNoTracking()
            .OrderBy(t => t.Key)
            .ThenBy(t => t.Locale)
            .Select(t => new EmailTemplateReadModel(
                t.Id,
                t.Key,
                t.Locale,
                t.Subject,
                t.HtmlBody,
                t.TextBody,
                t.IsActive,
                t.CreatedOnUtc,
                t.ModifiedOnUtc))
            .ToListAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<EmailTemplate?> GetByKeyAsync(string key, string locale, CancellationToken cancellationToken = default) {
        if (synchronizeTransactionAsync is not null) {
            await synchronizeTransactionAsync(cancellationToken).ConfigureAwait(false);
        }
        return await emailTemplates
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Key == key && t.Locale == locale, cancellationToken).ConfigureAwait(false);
    }

    public async Task<EmailTemplate> UpsertAsync(
        string key,
        string locale,
        string subject,
        string htmlBody,
        string textBody,
        bool isActive,
        CancellationToken cancellationToken = default) {
        if (synchronizeTransactionAsync is not null) {
            await synchronizeTransactionAsync(cancellationToken).ConfigureAwait(false);
        }
        EmailTemplate? existing = await emailTemplates
            .FirstOrDefaultAsync(t => t.Key == key && t.Locale == locale, cancellationToken).ConfigureAwait(false);

        if (existing is null) {
            var template = EmailTemplate.Create(key, locale, subject, htmlBody, textBody, isActive);
            await emailTemplates.AddAsync(template, cancellationToken).ConfigureAwait(false);
            return template;
        }

        existing.Update(subject, htmlBody, textBody, isActive);
        await Task.CompletedTask.ConfigureAwait(false);
        return existing;
    }
}
