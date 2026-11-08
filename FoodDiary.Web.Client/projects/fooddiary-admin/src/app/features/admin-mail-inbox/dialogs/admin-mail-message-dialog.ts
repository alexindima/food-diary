import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, computed, DestroyRef, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { RouterLink } from '@angular/router';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button';
import { FdUiDialogComponent } from 'fd-ui-kit/dialog/fd-ui-dialog';
import { FD_UI_DIALOG_DATA } from 'fd-ui-kit/dialog/fd-ui-dialog-data';
import { FdUiSegmentedToggleComponent } from 'fd-ui-kit/segmented-toggle/fd-ui-segmented-toggle';

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
    onRead: (id: AdminId<'mail-inbox-message'>, readAtUtc: AdminUtcInstant) => void;
};
@Component({
    selector: 'fd-admin-mail-message-dialog',
    imports: [CommonModule, RouterLink, TranslatePipe, FdUiButtonComponent, FdUiDialogComponent, FdUiSegmentedToggleComponent],
    templateUrl: './admin-mail-message-dialog.html',
    styleUrl: './admin-mail-message-dialog.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AdminMailMessageDialogComponent {
    protected readonly data = inject<AdminMailMessageDialogData>(FD_UI_DIALOG_DATA);
    private readonly mailInboxFacade = inject(AdminMailInboxFacade);
    private readonly destroyRef = inject(DestroyRef);
    protected readonly selectedMessage = signal<AdminMailInboxMessageDetails | null>(null);
    protected readonly isDetailsLoading = signal(true);
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
        const message = this.selectedMessage();
        return mailMessageContent(message?.htmlBody, message?.textBody);
    });
    protected readonly selectedMessageDetails = computed<AdminMailInboxMessageDetailsViewModel | null>(() => {
        const message = this.selectedMessage();
        if (message === null) {
            return null;
        }

        return {
            ...message,
            categoryLabel: this.formatCategory(message.category),
            readStateLabel:
                message.readAtUtc !== null && message.readAtUtc !== undefined
                    ? 'ADMIN_MAIL_INBOX.MESSAGE_READ'
                    : 'ADMIN_MAIL_INBOX.MESSAGE_UNREAD',
            toRecipientsLabel: this.formatRecipients(message.toRecipients),
        };
    });
    protected readonly selectedBody = computed(() => {
        const message = this.selectedMessage();
        if (message === null) {
            return '';
        }

        if (this.selectedBodyMode() === 'html') {
            return message.htmlBody ?? '';
        }

        if (this.selectedBodyMode() === 'raw') {
            return message.rawMime ?? '';
        }

        return this.content().textBody;
    });
    public constructor() {
        this.loadMessage();
    }
    protected setBodyMode(mode: string): void {
        this.selectedBodyMode.set(mode);
    }

    protected retryRead(): void {
        const message = this.selectedMessage();
        if (message !== null) {
            this.markMessageRead(message.id);
        }
    }
    private loadMessage(): void {
        this.isDetailsLoading.set(true);
        this.selectedBodyMode.set('preview');
        this.mailInboxFacade
            .getMessage(this.data.id)
            .pipe(takeUntilDestroyed(this.destroyRef))
            .subscribe({
                next: response => {
                    this.selectedMessage.set(response);
                    if (response.readAtUtc === null || response.readAtUtc === undefined) {
                        this.markMessageRead(response.id);
                    }

                    this.isDetailsLoading.set(false);
                },
                error: () => {
                    this.selectedMessage.set(null);
                    this.isDetailsLoading.set(false);
                },
            });
    }

    private formatRecipients(recipients: string[]): string {
        return recipients.length > 0 ? recipients.join(', ') : '-';
    }

    private formatCategory(category: string): string {
        return category === 'dmarc-report' ? 'DMARC' : 'ADMIN_MAIL_INBOX.MESSAGE_MAIL';
    }

    private markMessageRead(id: AdminId<'mail-inbox-message'>): void {
        if (this.markingRead()) {
            return;
        }
        this.markingRead.set(true);
        this.mailInboxFacade
            .markMessageRead(id)
            .pipe(takeUntilDestroyed(this.destroyRef))
            .subscribe({
                next: () => {
                    this.markingRead.set(false);
                    this.readFailed.set(false);
                    const readAtUtc = adminUtcInstant(new Date().toISOString());
                    this.data.onRead(id, readAtUtc);
                    this.selectedMessage.update(message => (message?.id === id ? { ...message, readAtUtc } : message));
                },
                error: () => {
                    this.markingRead.set(false);
                    this.readFailed.set(true);
                },
            });
    }
}
