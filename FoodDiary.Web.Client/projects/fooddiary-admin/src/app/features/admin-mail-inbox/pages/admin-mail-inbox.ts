import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, computed, DestroyRef, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button';
import { FdUiDialogService } from 'fd-ui-kit/dialog/fd-ui-dialog.service';
import { FdUiInputComponent } from 'fd-ui-kit/input/fd-ui-input';
import { FdUiPaginationComponent } from 'fd-ui-kit/pagination/fd-ui-pagination';
import { FdUiSelectComponent } from 'fd-ui-kit/select/fd-ui-select';
import type { Subscription } from 'rxjs';

import { adminPeriod, adminUtcPeriod } from '../../../shared/period/admin-period';
import { AdminPeriodControlComponent } from '../../../shared/period/admin-period-control';
import { adminPage } from '../../../shared/period/admin-query';
import { AdminMailMessageDialogComponent } from '../dialogs/admin-mail-message-dialog';
import { AdminMailInboxFacade } from '../lib/admin-mail-inbox.facade';
import type { AdminMailInboxMessagePage, AdminMailInboxMessageSummary } from '../models/admin-mail-inbox.data';

type AdminMailInboxMessageSummaryViewModel = {
    categoryLabel: string;
    readStateLabel: string;
} & AdminMailInboxMessageSummary;

const DEFAULT_MAIL_INBOX_LIMIT = 50;

@Component({
    selector: 'fd-admin-mail-inbox',
    imports: [
        AdminPeriodControlComponent,
        RouterLink,
        CommonModule,
        FdUiButtonComponent,
        FdUiInputComponent,
        FdUiSelectComponent,
        TranslatePipe,
        FdUiPaginationComponent,
    ],
    templateUrl: './admin-mail-inbox.html',
    styleUrl: './admin-mail-inbox.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AdminMailInboxComponent {
    private readonly dialogService = inject(FdUiDialogService);
    private readonly mailInboxFacade = inject(AdminMailInboxFacade);
    private readonly destroyRef = inject(DestroyRef);

    private readonly route = inject(ActivatedRoute);
    private readonly router = inject(Router);
    protected readonly subjectFilter = signal('');
    protected readonly senderFilter = signal('');
    protected readonly recordId = signal<string | null>(null);
    private listRequest?: Subscription;
    protected readonly loadFailed = signal(false);
    protected readonly messages = signal<AdminMailInboxMessageSummary[]>([]);
    protected readonly isLoading = signal(false);
    protected readonly pageSize = DEFAULT_MAIL_INBOX_LIMIT;
    protected readonly page = signal(1);
    protected readonly totalItems = signal(0);
    protected readonly unreadCount = signal<number | null>(null);
    protected readonly readCount = signal<number | null>(null);
    protected readonly rangeStart = computed(() => (this.messages().length === 0 ? 0 : (this.page() - 1) * this.pageSize + 1));
    protected readonly rangeEnd = computed(() => (this.messages().length === 0 ? 0 : this.rangeStart() + this.messages().length - 1));
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
        this.route.queryParamMap.pipe(takeUntilDestroyed()).subscribe(params => {
            this.page.set(adminPage(params.get('page')));
            this.recipientFilter.set(params.get('recipient') ?? '');
            const category = params.get('category');
            this.categoryFilter.set(category === 'general' || category === 'dmarc-report' ? category : 'all');
            const unread = params.get('unread');
            this.unreadFilter.set(unread === 'true' ? 'unread' : unread === 'false' ? 'read' : 'all');
            this.subjectFilter.set(params.get('search') ?? '');
            this.senderFilter.set(params.get('fromAddress') ?? '');
            this.recordId.set(params.get('id'));
            this.loadMessages();
        });
    }

    protected loadMessages(resetPage = false): void {
        if (resetPage) {
            this.applyFilters();
            return;
        }
        this.listRequest?.unsubscribe();
        this.loadFailed.set(false);
        this.unreadCount.set(null);
        this.readCount.set(null);
        const range = adminPeriod(this.route.snapshot.queryParamMap);
        if (range === null) {
            this.messages.set([]);
            this.totalItems.set(0);
            this.isLoading.set(false);
            return;
        }
        this.isLoading.set(true);
        this.listRequest = this.mailInboxFacade
            .getMessagePage(this.page(), this.pageSize, {
                ...adminUtcPeriod(range),
                search: this.subjectFilter(),
                fromAddress: this.senderFilter(),
                id: this.recordId() ?? '',
                recipient: this.recipientFilter(),
                category: this.categoryFilter() === 'all' ? '' : this.categoryFilter(),
                unread: this.unreadFilter() === 'all' ? undefined : this.unreadFilter() === 'unread',
            })
            .pipe(takeUntilDestroyed(this.destroyRef))
            .subscribe({
                next: response => {
                    this.applyPage(response);
                },
                error: () => {
                    this.loadFailed.set(true);
                    this.messages.set([]);
                    this.totalItems.set(0);
                    this.isLoading.set(false);
                },
            });
    }

    private applyFilters(): void {
        void this.router.navigate([], {
            relativeTo: this.route,
            queryParamsHandling: 'merge',
            replaceUrl: true,
            queryParams: {
                page: 1,
                id: null,
                recipient: this.recipientFilter(),
                category: this.categoryFilter(),
                search: this.subjectFilter(),
                fromAddress: this.senderFilter(),
                unread: this.unreadFilter() === 'all' ? null : this.unreadFilter() === 'unread',
            },
        });
    }

    private applyPage(response: AdminMailInboxMessagePage): void {
        this.totalItems.set(response.totalItems);
        this.unreadCount.set(response.unreadCount ?? null);
        this.readCount.set(response.readCount ?? null);
        const lastPage = Math.max(1, Math.ceil(response.totalItems / this.pageSize));
        if (this.page() > lastPage) {
            this.goToPage(lastPage - 1);
            return;
        }
        this.messages.set(response.items);
        this.isLoading.set(false);
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
                    this.loadMessages();
                },
            },
        });
    }

    protected goToPage(index: number): void {
        void this.router.navigate([], { relativeTo: this.route, queryParams: { page: index + 1 }, queryParamsHandling: 'merge' });
    }

    protected setCategoryFilter(value: string | null): void {
        this.categoryFilter.set(value === 'dmarc-report' || value === 'general' ? value : 'all');
        this.loadMessages(true);
    }

    private formatCategory(category: string): string {
        return category === 'dmarc-report' ? 'DMARC' : 'Mail';
    }

    private formatReadState(readAtUtc: string | null | undefined): string {
        return readAtUtc === null || readAtUtc === undefined ? 'Unread' : 'Read';
    }
}
