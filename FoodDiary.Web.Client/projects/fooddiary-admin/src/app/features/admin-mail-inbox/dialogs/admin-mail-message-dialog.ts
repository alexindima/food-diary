import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, computed, DestroyRef, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { RouterLink } from '@angular/router';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button';
import { FdUiDialogComponent } from 'fd-ui-kit/dialog/fd-ui-dialog';
import { FD_UI_DIALOG_DATA } from 'fd-ui-kit/dialog/fd-ui-dialog-data';

import { AdminMailInboxFacade } from '../lib/admin-mail-inbox.facade';
import type { AdminMailInboxMessageDetails } from '../models/admin-mail-inbox.data';

type AdminMailInboxMessageDetailsViewModel = AdminMailInboxMessageDetails & {
    categoryLabel: string;
    readStateLabel: string;
    toRecipientsLabel: string;
};
export type AdminMailMessageDialogData = { id: string; subject?: string | null; onRead: (id: string, readAtUtc: string) => void };
@Component({
    selector: 'fd-admin-mail-message-dialog',
    imports: [CommonModule, RouterLink, TranslatePipe, FdUiButtonComponent, FdUiDialogComponent],
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
    protected readonly selectedBodyMode = signal<'text' | 'html' | 'raw'>('text');
    protected readonly selectedMessageDetails = computed<AdminMailInboxMessageDetailsViewModel | null>(() => {
        const message = this.selectedMessage();
        if (message === null) {
            return null;
        }

        return {
            ...message,
            categoryLabel: this.formatCategory(message.category),
            readStateLabel: this.formatReadState(message.readAtUtc),
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

        return message.textBody ?? '';
    });
    public constructor() {
        this.loadMessage();
    }
    protected setBodyMode(mode: 'text' | 'html' | 'raw'): void {
        this.selectedBodyMode.set(mode);
    }
    private loadMessage(): void {
        this.isDetailsLoading.set(true);
        this.selectedBodyMode.set('text');
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
        return category === 'dmarc-report' ? 'DMARC' : 'Mail';
    }

    private formatReadState(readAtUtc: string | null | undefined): string {
        return readAtUtc === null || readAtUtc === undefined ? 'Unread' : 'Read';
    }

    private markMessageRead(id: string): void {
        this.mailInboxFacade
            .markMessageRead(id)
            .pipe(takeUntilDestroyed(this.destroyRef))
            .subscribe({
                next: () => {
                    const readAtUtc = new Date().toISOString();
                    this.data.onRead(id, readAtUtc);
                    this.selectedMessage.update(message => (message?.id === id ? { ...message, readAtUtc } : message));
                },
            });
    }
}
