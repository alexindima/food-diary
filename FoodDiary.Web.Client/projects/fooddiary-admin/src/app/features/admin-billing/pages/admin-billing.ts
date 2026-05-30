import { CommonModule, formatDate } from '@angular/common';
import { ChangeDetectionStrategy, Component, computed, DestroyRef, inject, LOCALE_ID, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button';
import { FdUiPaginationComponent } from 'fd-ui-kit/pagination/fd-ui-pagination';

import {
    type AdminBillingFilters,
    type AdminBillingPayment,
    AdminBillingService,
    type AdminBillingSubscription,
    type AdminBillingTab,
    type AdminBillingWebhookEvent,
    type PagedResponse,
} from '../api/admin-billing.service';
import type {
    AdminBillingPaymentViewModel,
    AdminBillingSubscriptionViewModel,
    AdminBillingWebhookEventViewModel,
} from './admin-billing.types';
import { AdminBillingFiltersComponent } from './admin-billing-filters';
import { AdminBillingMetadataPanelComponent } from './admin-billing-metadata-panel';
import { AdminBillingPaymentsTableComponent } from './admin-billing-payments-table';
import { AdminBillingSubscriptionsTableComponent } from './admin-billing-subscriptions-table';
import { AdminBillingWebhooksTableComponent } from './admin-billing-webhooks-table';

const DEFAULT_PAGE_SIZE = 20;
const SHORT_ID_MIN_LENGTH = 18;
const SHORT_ID_PREFIX_LENGTH = 8;
const SHORT_ID_SUFFIX_START = -6;

@Component({
    selector: 'fd-admin-billing',
    imports: [
        CommonModule,
        FdUiButtonComponent,
        FdUiPaginationComponent,
        AdminBillingFiltersComponent,
        AdminBillingSubscriptionsTableComponent,
        AdminBillingPaymentsTableComponent,
        AdminBillingWebhooksTableComponent,
        AdminBillingMetadataPanelComponent,
    ],
    templateUrl: './admin-billing.html',
    styleUrl: './admin-billing.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AdminBillingComponent {
    private readonly billingService = inject(AdminBillingService);
    private readonly destroyRef = inject(DestroyRef);
    private readonly locale = inject(LOCALE_ID);
    private loadRequestId = 0;

    protected readonly activeTab = signal<AdminBillingTab>('subscriptions');
    protected readonly subscriptions = signal<AdminBillingSubscription[]>([]);
    protected readonly payments = signal<AdminBillingPayment[]>([]);
    protected readonly webhookEvents = signal<AdminBillingWebhookEvent[]>([]);
    protected readonly subscriptionItems = computed<AdminBillingSubscriptionViewModel[]>(() =>
        this.subscriptions().map(subscription => ({
            ...subscription,
            currentPeriodStartText: this.formatDateLabel(subscription.currentPeriodStartUtc, 'shortDate'),
            currentPeriodEndText: this.formatDateLabel(subscription.currentPeriodEndUtc, 'shortDate'),
            nextBillingAttemptText: this.formatDateLabel(subscription.nextBillingAttemptUtc),
            updatedText: this.formatDateLabel(subscription.lastSyncedAtUtc ?? subscription.modifiedOnUtc ?? subscription.createdOnUtc),
            externalCustomerIdText: this.shortId(subscription.externalCustomerId),
            externalSubscriptionIdText: this.shortId(subscription.externalSubscriptionId),
            externalPaymentMethodIdText: this.shortId(subscription.externalPaymentMethodId),
        })),
    );
    protected readonly paymentItems = computed<AdminBillingPaymentViewModel[]>(() =>
        this.payments().map(payment => ({
            ...payment,
            createdText: this.formatDateLabel(payment.createdOnUtc),
            amountText: this.formatMoney(payment.amount, payment.currency),
            externalPaymentIdText: this.shortId(payment.externalPaymentId),
            externalCustomerIdText: this.shortId(payment.externalCustomerId),
            webhookEventIdText: this.shortId(payment.webhookEventId),
        })),
    );
    protected readonly webhookEventItems = computed<AdminBillingWebhookEventViewModel[]>(() =>
        this.webhookEvents().map(event => ({
            ...event,
            processedText: this.formatDateLabel(event.processedAtUtc),
            eventIdText: this.shortId(event.eventId),
            externalObjectIdText: this.shortId(event.externalObjectId),
        })),
    );
    protected readonly selectedMetadata = signal<string | null>(null);
    protected readonly totalPages = signal(1);
    protected readonly totalItems = signal(0);
    protected readonly page = signal(1);
    protected readonly limit = DEFAULT_PAGE_SIZE;
    protected readonly isLoading = signal(false);
    protected readonly errorMessage = signal<string | null>(null);
    protected readonly provider = signal('');
    protected readonly status = signal('');
    protected readonly kind = signal('');
    protected readonly search = signal('');
    protected readonly fromDate = signal('');
    protected readonly toDate = signal('');

    public constructor() {
        this.load();
    }

    protected setTab(tab: AdminBillingTab): void {
        this.activeTab.set(tab);
        this.page.set(1);
        this.selectedMetadata.set(null);
        this.load();
    }

    protected applyFilters(): void {
        this.page.set(1);
        this.selectedMetadata.set(null);
        this.load();
    }

    protected resetFilters(): void {
        this.provider.set('');
        this.status.set('');
        this.kind.set('');
        this.search.set('');
        this.fromDate.set('');
        this.toDate.set('');
        this.applyFilters();
    }

    protected goToPage(page: number): void {
        if (page < 1 || page > this.totalPages()) {
            return;
        }

        this.page.set(page);
        this.load();
    }

    protected showMetadata(value?: string | null): void {
        this.selectedMetadata.set(value !== null && value !== undefined && value.length > 0 ? this.formatJson(value) : null);
    }

    private formatMoney(amount?: number | null, currency?: string | null): string {
        if (amount === null || amount === undefined) {
            return '-';
        }

        return currency !== null && currency !== undefined && currency.length > 0 ? `${amount.toFixed(2)} ${currency}` : amount.toFixed(2);
    }

    private formatDateLabel(value?: string | Date | null, format = 'short'): string {
        return value !== null && value !== undefined ? formatDate(value, format, this.locale) : '-';
    }

    private shortId(value?: string | null): string {
        if (value === null || value === undefined || value.length === 0) {
            return '-';
        }

        return value.length > SHORT_ID_MIN_LENGTH
            ? `${value.slice(0, SHORT_ID_PREFIX_LENGTH)}...${value.slice(SHORT_ID_SUFFIX_START)}`
            : value;
    }

    protected load(): void {
        const requestId = ++this.loadRequestId;
        const tab = this.activeTab();
        this.isLoading.set(true);
        this.errorMessage.set(null);
        const filters = this.buildFilters();

        if (tab === 'subscriptions') {
            this.billingService
                .getSubscriptions(this.page(), this.limit, filters)
                .pipe(takeUntilDestroyed(this.destroyRef))
                .subscribe({
                    next: response => {
                        if (!this.isCurrentLoad(requestId, tab)) {
                            return;
                        }

                        this.subscriptions.set(response.items);
                        this.applyPageData(response);
                    },
                    error: (error: unknown) => {
                        this.applyLoadError(requestId, tab, error);
                    },
                });
            return;
        }

        if (tab === 'payments') {
            this.billingService
                .getPayments(this.page(), this.limit, filters)
                .pipe(takeUntilDestroyed(this.destroyRef))
                .subscribe({
                    next: response => {
                        if (!this.isCurrentLoad(requestId, tab)) {
                            return;
                        }

                        this.payments.set(response.items);
                        this.applyPageData(response);
                    },
                    error: (error: unknown) => {
                        this.applyLoadError(requestId, tab, error);
                    },
                });
            return;
        }

        this.billingService
            .getWebhookEvents(this.page(), this.limit, filters)
            .pipe(takeUntilDestroyed(this.destroyRef))
            .subscribe({
                next: response => {
                    if (!this.isCurrentLoad(requestId, tab)) {
                        return;
                    }

                    this.webhookEvents.set(response.items);
                    this.applyPageData(response);
                },
                error: (error: unknown) => {
                    this.applyLoadError(requestId, tab, error);
                },
            });
    }

    private applyPageData<T>(response: PagedResponse<T>): void {
        this.totalPages.set(response.totalPages > 0 ? response.totalPages : 1);
        this.totalItems.set(response.totalItems);
        this.isLoading.set(false);
    }

    private applyLoadError(requestId: number, tab: AdminBillingTab, error: unknown): void {
        if (!this.isCurrentLoad(requestId, tab)) {
            return;
        }

        this.errorMessage.set(this.getErrorMessage(error));
        this.clearData(tab);
    }

    private clearData(tab: AdminBillingTab): void {
        if (tab === 'subscriptions') {
            this.subscriptions.set([]);
        } else if (tab === 'payments') {
            this.payments.set([]);
        } else {
            this.webhookEvents.set([]);
        }

        this.totalPages.set(1);
        this.totalItems.set(0);
        this.isLoading.set(false);
    }

    private isCurrentLoad(requestId: number, tab: AdminBillingTab): boolean {
        return requestId === this.loadRequestId && tab === this.activeTab();
    }

    private getErrorMessage(error: unknown): string {
        if (error instanceof Error && error.message.length > 0) {
            return error.message;
        }

        return 'Failed to load billing records.';
    }

    private buildFilters(): AdminBillingFilters {
        const provider = this.provider().trim();
        const status = this.status().trim();
        const kind = this.kind().trim();
        const search = this.search().trim();
        return {
            provider: provider.length > 0 ? provider : null,
            status: status.length > 0 ? status : null,
            kind: this.activeTab() === 'payments' && kind.length > 0 ? kind : null,
            search: search.length > 0 ? search : null,
            fromUtc: this.toUtcStart(this.fromDate()),
            toUtc: this.toUtcEnd(this.toDate()),
        };
    }

    private toUtcStart(value: string): string | null {
        return value.length > 0 ? new Date(`${value}T00:00:00.000Z`).toISOString() : null;
    }

    private toUtcEnd(value: string): string | null {
        return value.length > 0 ? new Date(`${value}T23:59:59.999Z`).toISOString() : null;
    }

    private formatJson(value: string): string {
        try {
            return JSON.stringify(JSON.parse(value), null, 2);
        } catch {
            return value;
        }
    }
}
