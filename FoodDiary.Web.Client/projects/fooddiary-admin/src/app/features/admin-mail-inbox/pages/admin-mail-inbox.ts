import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, computed, DestroyRef, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button';
import { FdUiDialogService } from 'fd-ui-kit/dialog/fd-ui-dialog.service';
import { fdUiCoerceInputTextValue, FdUiInputComponent, type FdUiInputValue } from 'fd-ui-kit/input/fd-ui-input';
import { FdUiSelectComponent } from 'fd-ui-kit/select/fd-ui-select';
import type { Subscription } from 'rxjs';

import { AdminMailMessageDialogComponent } from '../dialogs/admin-mail-message-dialog';
import { AdminMailInboxFacade } from '../lib/admin-mail-inbox.facade';
import type { AdminMailInboxMessageSummary } from '../models/admin-mail-inbox.data';

type AdminMailInboxMessageSummaryViewModel = {
    categoryLabel: string;
    readStateLabel: string;
} & AdminMailInboxMessageSummary;

const DEFAULT_MAIL_INBOX_LIMIT = 50;
const MAX_MAIL_INBOX_LIMIT = 200;

@Component({
    selector: 'fd-admin-mail-inbox',
    imports: [CommonModule, FdUiButtonComponent, FdUiInputComponent, FdUiSelectComponent, TranslatePipe],
    templateUrl: './admin-mail-inbox.html',
    styleUrl: './admin-mail-inbox.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AdminMailInboxComponent {
    private readonly dialogService = inject(FdUiDialogService);
    private readonly mailInboxFacade = inject(AdminMailInboxFacade);
    private readonly destroyRef = inject(DestroyRef);

    private listRequest?: Subscription;
    protected readonly loadFailed = signal(false);
    protected readonly messages = signal<AdminMailInboxMessageSummary[]>([]);
    protected readonly isLoading = signal(false);
    protected readonly limit = signal(DEFAULT_MAIL_INBOX_LIMIT);
    protected readonly recipientFilter = signal('');
    protected readonly unreadFilter = signal<string | null>('all');
    protected readonly categoryFilter = signal<'all' | 'dmarc-report' | 'general'>('all');
    protected readonly filteredMessages = computed<AdminMailInboxMessageSummaryViewModel[]>(() => {
        const messages = this.messages();

        return messages.map(message => ({
            ...message,
            categoryLabel: this.formatCategory(message.category),
            readStateLabel: this.formatReadState(message.readAtUtc),
        }));
    });
    public constructor() {
        this.loadMessages();
    }

    protected loadMessages(): void {
        this.listRequest?.unsubscribe();
        this.loadFailed.set(false);
        this.isLoading.set(true);
        this.listRequest = this.mailInboxFacade
            .getMessages(
                this.limit(),
                this.recipientFilter(),
                this.categoryFilter() === 'all' ? '' : this.categoryFilter(),
                this.unreadFilter() === 'all' ? undefined : this.unreadFilter() === 'unread',
            )
            .pipe(takeUntilDestroyed(this.destroyRef))
            .subscribe({
                next: response => {
                    this.messages.set(response);
                    this.isLoading.set(false);
                },
                error: () => {
                    this.loadFailed.set(true);
                    this.messages.set([]);
                    this.isLoading.set(false);
                },
            });
    }

    protected selectMessage(message: AdminMailInboxMessageSummary): void {
        this.dialogService.open(AdminMailMessageDialogComponent, {
            preset: 'detail',
            panelClass: 'fd-admin-mail-message-dialog',
            size: 'xl',
            data: {
                id: message.id,
                subject: message.subject,
                onRead: (id: string, readAtUtc: string) => {
                    this.messages.update(messages => messages.map(item => (item.id === id ? { ...item, readAtUtc } : item)));
                },
            },
        });
    }

    protected updateLimit(value: FdUiInputValue): void {
        const parsed = Number.parseInt(fdUiCoerceInputTextValue(value), 10);
        if (!Number.isFinite(parsed)) {
            return;
        }

        this.limit.set(Math.max(1, Math.min(parsed, MAX_MAIL_INBOX_LIMIT)));
    }

    protected setCategoryFilter(value: string | null): void {
        this.categoryFilter.set(value === 'dmarc-report' || value === 'general' ? value : 'all');
        this.loadMessages();
    }

    private formatCategory(category: string): string {
        return category === 'dmarc-report' ? 'DMARC' : 'Mail';
    }

    private formatReadState(readAtUtc: string | null | undefined): string {
        return readAtUtc === null || readAtUtc === undefined ? 'Unread' : 'Read';
    }
}
