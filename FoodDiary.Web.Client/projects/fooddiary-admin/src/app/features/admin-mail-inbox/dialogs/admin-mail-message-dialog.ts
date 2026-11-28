import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, computed, DestroyRef, effect, inject, signal, untracked } from '@angular/core';
import { rxResource, takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { RouterLink } from '@angular/router';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button';
import { FdUiDialogComponent } from 'fd-ui-kit/dialog/fd-ui-dialog';
import { FD_UI_DIALOG_DATA } from 'fd-ui-kit/dialog/fd-ui-dialog-data';
import { FdUiSegmentedToggleComponent } from 'fd-ui-kit/segmented-toggle/fd-ui-segmented-toggle';
import { take } from 'rxjs';

import { AdminLoadErrorComponent } from '../../../shared/feedback/admin-load-error';
import { type AdminId, type AdminUtcInstant, adminUtcInstant } from '../../../shared/models/semantics/admin-meaning';
import { AdminMailInboxFacade } from '../lib/admin-mail-inbox.facade';
import type { AdminMailInboxMessageDetails } from '../models/admin-mail-inbox.data';
import { mailMessageContent } from './mail-message-content';

type AdminMailInboxMessageDetailsViewModel = AdminMailInboxMessageDetails & {
    categoryLabel: string;
    readStateLabel: string;
    toRecipientsLabel: string;
};
export type AdminMailMessageDialogData = {
    id: AdminId<'mail-inbox-message'>;
    subject?: string | null;
    onRead?: (id: AdminId<'mail-inbox-message'>, readAtUtc: AdminUtcInstant) => void;
};
@Component({
    selector: 'fd-admin-mail-message-dialog',
    imports: [
        CommonModule,
        RouterLink,
        TranslatePipe,
        FdUiButtonComponent,
        FdUiDialogComponent,
        FdUiSegmentedToggleComponent,
        AdminLoadErrorComponent,
    ],
    templateUrl: './admin-mail-message-dialog.html',
    styleUrl: './admin-mail-message-dialog.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AdminMailMessageDialogComponent {
    protected readonly data = inject<AdminMailMessageDialogData>(FD_UI_DIALOG_DATA);
    private readonly mailInboxFacade = inject(AdminMailInboxFacade);
    private readonly destroyRef = inject(DestroyRef);
    private readonly messageResource = rxResource({
        params: () => this.data.id,
        stream: ({ params }) => this.mailInboxFacade.getMessage(params).pipe(take(1)),
    });
    private readonly message = computed(() => (this.messageResource.hasValue() ? this.messageResource.value() : null));
    private readonly acknowledgedReadAtUtc = signal<AdminUtcInstant | null>(null);
    protected readonly isDetailsLoading = this.messageResource.isLoading;
    protected readonly readFailed = signal(false);
    protected readonly markingRead = signal(false);
    protected readonly selectedBodyMode = signal('preview');
    protected readonly bodyModes = [
        { value: 'preview', labelKey: 'ADMIN_MAIL_INBOX.VIEW_MESSAGE' },
        { value: 'text', labelKey: 'ADMIN_MAIL_INBOX.VIEW_TEXT' },
        { value: 'html', labelKey: 'ADMIN_MAIL_INBOX.VIEW_HTML' },
        { value: 'raw', labelKey: 'ADMIN_MAIL_INBOX.VIEW_RAW' },
    ];
    protected readonly content = computed(() => {
        const message = this.message();
        return mailMessageContent(message?.htmlBody, message?.textBody);
    });
    protected readonly selectedMessageDetails = computed<AdminMailInboxMessageDetailsViewModel | null>(() => {
        const message = this.message();
        if (message === null) {
            return null;
        }

        const readAtUtc = this.acknowledgedReadAtUtc() ?? message.readAtUtc;
        return {
            ...message,
            readAtUtc,
            categoryLabel: this.formatCategory(message.category),
            readStateLabel:
                readAtUtc !== null && readAtUtc !== undefined ? 'ADMIN_MAIL_INBOX.MESSAGE_READ' : 'ADMIN_MAIL_INBOX.MESSAGE_UNREAD',
            toRecipientsLabel: this.formatRecipients(message.toRecipients),
        };
    });
    public constructor() {
        effect(() => {
            const message = this.message();
            if (message !== null && (message.readAtUtc === null || message.readAtUtc === undefined)) {
                untracked(() => {
                    this.markMessageRead(message.id);
                });
            }
        });
    }

    protected setBodyMode(mode: string): void {
        this.selectedBodyMode.set(mode);
    }

    protected retryDetails(): void {
        this.messageResource.reload();
    }

    private formatRecipients(recipients: string[]): string {
        return recipients.length > 0 ? recipients.join(', ') : '-';
    }

    private formatCategory(category: string): string {
        return category === 'dmarc-report' ? 'DMARC' : 'ADMIN_MAIL_INBOX.MESSAGE_MAIL';
    }

    protected markMessageRead(id: AdminId<'mail-inbox-message'>): void {
        if (this.markingRead()) {
            return;
        }
        this.markingRead.set(true);
        this.mailInboxFacade
            .markMessageRead(id)
            .pipe(takeUntilDestroyed(this.destroyRef))
            .subscribe({
                next: () => {
                    const readAtUtc = adminUtcInstant(new Date().toISOString());
                    this.acknowledgedReadAtUtc.set(readAtUtc);
                    this.markingRead.set(false);
                    this.readFailed.set(false);
                    this.data.onRead?.(id, readAtUtc);
                },
                error: () => {
                    this.markingRead.set(false);
                    this.readFailed.set(true);
                },
            });
    }
}
