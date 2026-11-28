import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, computed, inject, input, linkedSignal } from '@angular/core';
import { rxResource } from '@angular/core/rxjs-interop';
import { ActivatedRoute, convertToParamMap, Router, RouterLink } from '@angular/router';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiActivateDirective, FdUiStopActivationDirective } from 'fd-ui-kit';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button';
import { FdUiDialogService } from 'fd-ui-kit/dialog/fd-ui-dialog.service';
import { FdUiInputComponent } from 'fd-ui-kit/input/fd-ui-input';
import { FdUiPaginationComponent } from 'fd-ui-kit/pagination/fd-ui-pagination';
import { FdUiSelectComponent } from 'fd-ui-kit/select/fd-ui-select';
import { map } from 'rxjs';

import { connectAdminDevelopmentScreen } from '../../../shared/development/admin-development-screen';
import type { AdminId, AdminUtcInstant } from '../../../shared/models/semantics/admin-meaning';
import { adminRouteInputsMatch, restoreAdminPage } from '../../../shared/period/admin-pagination';
import { adminPeriod, adminUtcPeriod } from '../../../shared/period/admin-period';
import { AdminPeriodControlComponent } from '../../../shared/period/admin-period-control';
import { adminPage, firstAdminQueryValue } from '../../../shared/period/admin-query';
import { AdminMailboxViewsComponent } from '../components/admin-mailbox-views';
import { AdminMailMessageDialogComponent } from '../dialogs/admin-mail-message-dialog';
import { AdminMailInboxFacade } from '../lib/admin-mail-inbox.facade';
import { AdminMailInboxPreferences, DMARC_MAILBOX, FEEDBACK_MAILBOX, type IncomingMailbox } from '../lib/admin-mail-inbox-preferences';
import type { AdminMailInboxFilters, AdminMailInboxMessagePage, AdminMailInboxMessageSummary } from '../models/admin-mail-inbox.data';

type AdminMailInboxMessageSummaryViewModel = {
    categoryLabel: string;
    readStateLabel: string;
} & AdminMailInboxMessageSummary;

type InboxFilterDraft = {
    search: string;
    fromAddress: string;
    recipient: string;
    category: 'inbox' | 'all' | 'dmarc-report' | 'general';
    unread: string | null;
};

type InboxPageRequest = { page: number; filters: AdminMailInboxFilters };

const DEFAULT_MAIL_INBOX_LIMIT = 50;

@Component({
    selector: 'fd-admin-mail-inbox',
    imports: [
        FdUiStopActivationDirective,
        FdUiActivateDirective,
        AdminPeriodControlComponent,
        RouterLink,
        CommonModule,
        FdUiButtonComponent,
        AdminMailboxViewsComponent,
        FdUiInputComponent,
        FdUiSelectComponent,
        TranslatePipe,
        FdUiPaginationComponent,
    ],
    providers: [AdminMailInboxPreferences],
    templateUrl: './admin-mail-inbox.html',
    styleUrl: './admin-mail-inbox.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AdminMailInboxComponent {
    public readonly feedbackMailbox = input(false);
    public readonly page = input(undefined, { transform: firstAdminQueryValue });
    public readonly search = input(undefined, { transform: firstAdminQueryValue });
    public readonly recipient = input(undefined, { transform: firstAdminQueryValue });
    public readonly category = input(undefined, { transform: firstAdminQueryValue });
    public readonly unread = input(undefined, { transform: firstAdminQueryValue });
    public readonly fromAddress = input(undefined, { transform: firstAdminQueryValue });
    public readonly period = input(undefined, { transform: firstAdminQueryValue });
    public readonly from = input(undefined, { transform: firstAdminQueryValue });
    public readonly to = input(undefined, { transform: firstAdminQueryValue });
    public readonly id = input(undefined, { transform: firstAdminQueryValue });

    private readonly dialogService = inject(FdUiDialogService);
    private readonly mailInboxFacade = inject(AdminMailInboxFacade);
    private readonly preferences = inject(AdminMailInboxPreferences);
    private readonly route = inject(ActivatedRoute);
    private readonly router = inject(Router);
    protected readonly inboxPath = computed(() => (this.feedbackMailbox() ? '/feedback' : '/mail-inbox'));
    private readonly appliedQuery = computed(() => ({
        page: this.page(),
        search: this.search(),
        recipient: this.recipient(),
        category: this.category(),
        unread: this.unread(),
        fromAddress: this.fromAddress(),
        period: this.period(),
        from: this.from(),
        to: this.to(),
        id: this.id(),
    }));
    protected readonly recordId = computed(() => this.appliedQuery().id ?? null);
    protected readonly selectedRecipient = computed(() => (this.feedbackMailbox() ? FEEDBACK_MAILBOX : (this.recipient()?.trim() ?? '')));
    private readonly appliedCategory = computed<InboxFilterDraft['category']>(() => {
        const category = this.category();
        if (category === 'all' || category === 'general' || category === 'dmarc-report') {
            return category;
        }
        return 'inbox';
    });
    private readonly appliedFilters = computed<InboxFilterDraft>(() => {
        const { search, fromAddress, unread } = this.appliedQuery();
        return {
            search: search ?? '',
            fromAddress: fromAddress ?? '',
            recipient: this.selectedRecipient(),
            category: this.appliedCategory(),
            unread: unread === 'true' ? 'unread' : unread === 'false' ? 'read' : 'all',
        };
    });
    protected readonly hiddenRecipients = this.preferences.hiddenRecipients;
    private readonly excludedRecipients = computed(() => {
        if (this.recordId() !== null || this.selectedRecipient() !== '' || this.appliedCategory() === 'dmarc-report') {
            return;
        }
        const hidden = this.hiddenRecipients();
        if (this.appliedCategory() === 'inbox') {
            return [DMARC_MAILBOX, ...hidden];
        }
        return hidden.length > 0 ? hidden : undefined;
    });
    protected readonly subjectFilter = linkedSignal(() => this.appliedFilters().search);
    protected readonly senderFilter = linkedSignal(() => this.appliedFilters().fromAddress);
    protected readonly recipientFilter = linkedSignal(() => this.appliedFilters().recipient);
    protected readonly unreadFilter = linkedSignal(() => this.appliedFilters().unread);
    protected readonly categoryFilter = linkedSignal(() => this.appliedFilters().category);
    protected readonly pageSize = DEFAULT_MAIL_INBOX_LIMIT;
    protected readonly pageNumber = computed(() => adminPage(this.appliedQuery().page ?? null));
    private readonly requestedPage = linkedSignal(() => this.buildPageRequest());
    private readonly pageResource = rxResource({
        params: this.requestedPage,
        stream: ({ params }) =>
            this.mailInboxFacade.getMessagePage(params.page, this.pageSize, params.filters).pipe(
                map(response => {
                    if (this.requestedPage() !== params || !adminRouteInputsMatch(this.route, this.appliedQuery())) {
                        return null;
                    }
                    const correctingPage = restoreAdminPage(this.router, this.route, params.page, {
                        totalItems: response.totalItems,
                        pageSize: this.pageSize,
                    });
                    return correctingPage ? null : response;
                }),
            ),
    });
    protected readonly pageResult = computed(() => (this.pageResource.hasValue() ? this.pageResource.value() : null));
    protected readonly isLoading = computed(
        () => this.pageResource.isLoading() || (this.pageResource.hasValue() && this.pageResource.value() === null),
    );
    private readonly displayedPage = linkedSignal({
        source: () => ({ result: this.pageResult(), loading: this.isLoading() }),
        computation: ({ result, loading }, previous): AdminMailInboxMessagePage | null =>
            result ?? (loading ? (previous?.value ?? null) : null),
    });
    protected readonly loadFailed = computed(() => this.pageResource.error() !== undefined);
    protected readonly messages = computed(() => this.displayedPage()?.items ?? []);
    protected readonly totalItems = computed(() => this.displayedPage()?.totalItems ?? 0);
    protected readonly unreadCount = computed(() => this.pageResult()?.unreadCount ?? null);
    protected readonly readCount = computed(() => this.pageResult()?.readCount ?? null);
    protected readonly rangeStart = computed(() => (this.messages().length === 0 ? 0 : (this.pageNumber() - 1) * this.pageSize + 1));
    protected readonly rangeEnd = computed(() => (this.messages().length === 0 ? 0 : this.rangeStart() + this.messages().length - 1));
    protected readonly filteredMessages = computed<AdminMailInboxMessageSummaryViewModel[]>(() => {
        const messages = this.messages();

        return messages.map(message => ({
            ...message,
            categoryLabel: this.formatCategory(message.category),
            readStateLabel: this.formatReadState(message.readAtUtc),
        }));
    });
    public constructor() {
        const fields = ['page', 'search', 'recipient', 'category', 'unread', 'fromAddress', 'period', 'from', 'to'] as const;
        connectAdminDevelopmentScreen({
            kind: 'admin-mail-inbox',
            filterFields: fields,
            read: () => ({
                filters: {
                    ...Object.fromEntries(fields.map(field => [field, this.appliedQuery()[field] ?? null])),
                    ...(this.feedbackMailbox() ? { recipient: FEEDBACK_MAILBOX } : {}),
                },
                page: this.pageNumber(),
                totalItems: this.totalItems(),
                loading: this.isLoading(),
                error: this.loadFailed(),
                unreadCount: this.unreadCount(),
                readCount: this.readCount(),
                items: this.messages().map(message => ({
                    id: message.id,
                    status: message.status,
                    receivedAtUtc: message.receivedAtUtc,
                    readAtUtc: message.readAtUtc,
                })),
            }),
            setFiltersAsync: async params => {
                await this.router.navigate([], { relativeTo: this.route, queryParams: params });
            },
        });
    }

    protected loadMessages(resetPage = false): void {
        if (resetPage) {
            this.applyFilters();
            return;
        }
        this.requestedPage.set(this.buildPageRequest());
    }

    private buildPageRequest(): InboxPageRequest | undefined {
        const filters = this.appliedFilters();
        const { period, from, to } = this.appliedQuery();
        const range = adminPeriod(convertToParamMap({ period: period ?? null, from: from ?? null, to: to ?? null }));
        if (range === null) {
            return undefined;
        }
        return {
            page: this.pageNumber(),
            filters: {
                ...adminUtcPeriod(range),
                search: filters.search,
                fromAddress: filters.fromAddress,
                id: this.recordId() ?? '',
                recipient: filters.recipient,
                excludeRecipients: this.excludedRecipients(),
                category: filters.category === 'general' || filters.category === 'dmarc-report' ? filters.category : '',
                unread: filters.unread === 'all' ? undefined : filters.unread === 'unread',
            },
        };
    }

    protected selectRecipient(recipient: string): void {
        void this.router.navigate([], {
            relativeTo: this.route,
            queryParamsHandling: 'merge',
            queryParams: { page: 1, id: null, recipient: recipient === '' ? null : recipient },
        });
    }

    protected setRecipientHidden({ recipient, hidden }: { recipient: IncomingMailbox; hidden: boolean }): void {
        this.preferences.setHidden(recipient, hidden);
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
                category: this.categoryFilter() === 'inbox' ? null : this.categoryFilter(),
                search: this.subjectFilter(),
                fromAddress: this.senderFilter(),
                unread: this.unreadFilter() === 'all' ? null : this.unreadFilter() === 'unread',
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
                onRead: (id: AdminId<'mail-inbox-message'>, readAtUtc: AdminUtcInstant) => {
                    this.displayedPage.update(page =>
                        page === null ? null : { ...page, items: page.items.map(item => (item.id === id ? { ...item, readAtUtc } : item)) },
                    );
                    this.loadMessages();
                },
            },
        });
    }

    protected goToPage(index: number): void {
        void this.router.navigate([], { relativeTo: this.route, queryParams: { page: index + 1 }, queryParamsHandling: 'merge' });
    }

    protected setCategoryFilter(value: string | null): void {
        this.categoryFilter.set(value === 'dmarc-report' || value === 'general' || value === 'all' ? value : 'inbox');
        this.loadMessages(true);
    }

    private formatCategory(category: string): string {
        return category === 'dmarc-report' ? 'DMARC' : 'Mail';
    }

    private formatReadState(readAtUtc: AdminUtcInstant | null | undefined): string {
        return readAtUtc === null || readAtUtc === undefined ? 'Unread' : 'Read';
    }
}
