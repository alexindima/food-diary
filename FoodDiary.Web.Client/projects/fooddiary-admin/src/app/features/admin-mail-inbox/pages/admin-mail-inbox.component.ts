import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, computed, DestroyRef, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormsModule } from '@angular/forms';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button.component';
import { FdUiInputComponent } from 'fd-ui-kit/input/fd-ui-input.component';

import { AdminMailInboxService } from '../api/admin-mail-inbox.service';
import { type AdminMailInboxMessageDetails, type AdminMailInboxMessageSummary } from '../models/admin-mail-inbox.data';

@Component({
    selector: 'fd-admin-mail-inbox',
    standalone: true,
    imports: [CommonModule, FormsModule, FdUiButtonComponent, FdUiInputComponent],
    templateUrl: './admin-mail-inbox.component.html',
    styleUrl: './admin-mail-inbox.component.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AdminMailInboxComponent {
    private readonly mailInboxService = inject(AdminMailInboxService);
    private readonly destroyRef = inject(DestroyRef);

    public readonly messages = signal<AdminMailInboxMessageSummary[]>([]);
    public readonly selectedMessage = signal<AdminMailInboxMessageDetails | null>(null);
    public readonly isLoading = signal(false);
    public readonly isDetailsLoading = signal(false);
    public readonly limit = signal(50);
    public readonly categoryFilter = signal<'all' | 'dmarc-report' | 'general'>('all');
    public readonly selectedBodyMode = signal<'text' | 'html' | 'raw'>('text');
    public readonly filteredMessages = computed(() => {
        const category = this.categoryFilter();
        if (category === 'all') {
            return this.messages();
        }

        return this.messages().filter(message => message.category === category);
    });
    public readonly selectedBody = computed(() => {
        const message = this.selectedMessage();
        if (!message) {
            return '';
        }

        if (this.selectedBodyMode() === 'html') {
            return message.htmlBody || '';
        }

        if (this.selectedBodyMode() === 'raw') {
            return message.rawMime;
        }

        return message.textBody || '';
    });
    public readonly dmarcTotalMessages = computed(() => {
        const report = this.selectedMessage()?.dmarcReport;
        return report?.records.reduce((total, record) => total + record.count, 0) ?? 0;
    });
    public readonly dmarcProblemRecords = computed(() => {
        const report = this.selectedMessage()?.dmarcReport;
        return report?.records.filter(record => record.dkim !== 'pass' || record.spf !== 'pass').length ?? 0;
    });

    public constructor() {
        this.loadMessages();
    }

    public loadMessages(): void {
        this.isLoading.set(true);
        this.mailInboxService
            .getMessages(this.limit())
            .pipe(takeUntilDestroyed(this.destroyRef))
            .subscribe({
                next: response => {
                    this.messages.set(response);
                    this.isLoading.set(false);
                },
                error: () => {
                    this.messages.set([]);
                    this.isLoading.set(false);
                },
            });
    }

    public selectMessage(message: AdminMailInboxMessageSummary): void {
        this.isDetailsLoading.set(true);
        this.selectedBodyMode.set('text');
        this.mailInboxService
            .getMessage(message.id)
            .pipe(takeUntilDestroyed(this.destroyRef))
            .subscribe({
                next: response => {
                    this.selectedMessage.set(response);
                    this.isDetailsLoading.set(false);
                },
                error: () => {
                    this.selectedMessage.set(null);
                    this.isDetailsLoading.set(false);
                },
            });
    }

    public updateLimit(value: string): void {
        const parsed = Number.parseInt(value, 10);
        if (!Number.isFinite(parsed)) {
            return;
        }

        this.limit.set(Math.max(1, Math.min(parsed, 200)));
    }

    public setBodyMode(mode: 'text' | 'html' | 'raw'): void {
        this.selectedBodyMode.set(mode);
    }

    public setCategoryFilter(value: string): void {
        if (value === 'dmarc-report' || value === 'general') {
            this.categoryFilter.set(value);
            return;
        }

        this.categoryFilter.set('all');
    }

    public formatRecipients(recipients: string[]): string {
        return recipients.length > 0 ? recipients.join(', ') : '-';
    }

    public formatCategory(category: string): string {
        return category === 'dmarc-report' ? 'DMARC' : 'Mail';
    }
}
