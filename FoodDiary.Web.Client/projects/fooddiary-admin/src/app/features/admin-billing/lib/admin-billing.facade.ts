import { formatDate } from '@angular/common';
import { computed, inject, Injectable, LOCALE_ID, signal } from '@angular/core';
import { rxResource } from '@angular/core/rxjs-interop';
import { ActivatedRoute, Router } from '@angular/router';
import { catchError, map, type Observable, throwError } from 'rxjs';

import { restoreAdminPage } from '../../../shared/period/admin-pagination';
import { adminExclusiveDatePeriod, type AdminPeriod } from '../../../shared/period/admin-period';
import { adminQueryValue } from '../../../shared/period/admin-query';
import { AdminBillingService } from '../api/admin-billing.service';
import type {
    AdminBillingCancellationReport,
    AdminBillingFilters,
    AdminBillingPayment,
    AdminBillingSubscription,
    AdminBillingTab,
    AdminBillingWebhookEvent,
    PagedResponse,
} from '../models/admin-billing.models';
import type {
    AdminBillingPaymentViewModel,
    AdminBillingSubscriptionViewModel,
    AdminBillingWebhookEventViewModel,
} from '../models/admin-billing-view.models';

const DEFAULT_PAGE_SIZE = 20;
const SHORT_ID_MIN_LENGTH = 18;
const SHORT_ID_PREFIX_LENGTH = 8;
const SHORT_ID_SUFFIX_START = -6;
const PAYMENT_DECIMAL_PLACES = 3;
const BILLING_QUERY_FIELDS = ['tab', 'page', 'search', 'provider', 'status', 'kind', 'period', 'from', 'to'] as const;

type BillingRequest = {
    tab: AdminBillingTab;
    page: number;
    queryKey: string;
    filters: AdminBillingFilters;
    revenueFilters?: AdminBillingFilters;
};

type BillingRecords =
    | { tab: 'subscriptions'; data: PagedResponse<AdminBillingSubscription> }
    | { tab: 'payments'; data: PagedResponse<AdminBillingPayment> }
    | { tab: 'webhook-events'; data: PagedResponse<AdminBillingWebhookEvent> }
    | { tab: 'cancellations'; data: AdminBillingCancellationReport };

@Injectable()
export class AdminBillingFacade {
    private readonly billingService = inject(AdminBillingService);
    private readonly locale = inject(LOCALE_ID);
    private readonly router = inject(Router);
    private readonly route = inject(ActivatedRoute);
    private periodRange = { from: '', to: '' };
    private readonly requestedLoad = signal<BillingRequest | undefined>(undefined);
    private readonly recordsResource = rxResource({
        params: this.requestedLoad,
        stream: ({ params }) =>
            this.loadRecords(params).pipe(
                map(records => {
                    // The router commits its snapshot before updating the page inputs.
                    if (this.requestedLoad() !== params || this.activeTab() !== params.tab || this.queryKey() !== params.queryKey) {
                        return null;
                    }
                    const correctingPage = restoreAdminPage(this.router, this.route, params.page, {
                        totalItems: records.data.totalItems,
                        pageSize: this.limit,
                    });
                    return correctingPage ? null : records;
                }),
                catchError((error: unknown) =>
                    throwError(() =>
                        error instanceof Error && error.message.length > 0 ? error : new Error('Failed to load billing records.'),
                    ),
                ),
            ),
    });
    private readonly revenueResource = rxResource({
        params: () => this.requestedLoad()?.revenueFilters,
        stream: ({ params }) => this.billingService.getRevenueSummary(params),
    });
    private readonly records = computed(() => (this.recordsResource.hasValue() ? this.recordsResource.value() : null));
    private readonly cancellationReport = computed(() => {
        const records = this.records();
        return records?.tab === 'cancellations' ? records.data : null;
    });

    public readonly revenueSummary = computed(() => (this.revenueResource.hasValue() ? this.revenueResource.value() : null));
    public readonly revenueFailed = computed(() => this.revenueResource.error() !== undefined);
    public readonly cancellations = computed(() => this.cancellationReport()?.items ?? []);
    public readonly cancellationReasons = computed(() => this.cancellationReport()?.reasons ?? []);
    public readonly cancellationFeedbackCount = computed(() => this.cancellationReport()?.feedbackCount ?? 0);
    public readonly activeTab = signal<AdminBillingTab>('subscriptions');
    public readonly subscriptions = computed(() => {
        const records = this.records();
        return records?.tab === 'subscriptions' ? records.data.items : [];
    });
    public readonly payments = computed(() => {
        const records = this.records();
        return records?.tab === 'payments' ? records.data.items : [];
    });
    public readonly webhookEvents = computed(() => {
        const records = this.records();
        return records?.tab === 'webhook-events' ? records.data.items : [];
    });
    public readonly subscriptionItems = computed<AdminBillingSubscriptionViewModel[]>(() =>
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
    public readonly paymentItems = computed<AdminBillingPaymentViewModel[]>(() =>
        this.payments().map(payment => ({
            ...payment,
            createdText: this.formatDateLabel(payment.createdOnUtc),
            amountText: this.formatMoney(payment.amount, payment.currency),
            externalPaymentIdText: this.shortId(payment.externalPaymentId),
            externalCustomerIdText: this.shortId(payment.externalCustomerId),
            webhookEventIdText: this.shortId(payment.webhookEventId),
        })),
    );
    public readonly webhookEventItems = computed<AdminBillingWebhookEventViewModel[]>(() =>
        this.webhookEvents().map(event => ({
            ...event,
            processedText: this.formatDateLabel(event.processedAtUtc ?? event.receivedAtUtc ?? event.createdOnUtc),
            eventIdText: this.shortId(event.eventId),
            externalObjectIdText: this.shortId(event.externalObjectId),
        })),
    );
    public readonly selectedMetadata = signal<string | null>(null);
    public readonly totalPages = computed(() => {
        const total = this.records()?.data.totalPages ?? 1;
        return total > 0 ? total : 1;
    });
    public readonly totalItems = computed(() => this.records()?.data.totalItems ?? 0);
    public readonly page = signal(1);
    public readonly limit = DEFAULT_PAGE_SIZE;
    // A corrected page stays loading until its URL triggers the next request.
    public readonly isLoading = computed(
        () => this.recordsResource.isLoading() || (this.recordsResource.hasValue() && this.records() === null),
    );
    public readonly errorMessage = computed(() => this.recordsResource.error()?.message ?? null);
    public readonly provider = signal('');
    public readonly status = signal('');
    public readonly kind = signal('');
    public readonly search = signal('');
    public readonly fromDate = signal('');
    public readonly toDate = signal('');

    public setTab(tab: AdminBillingTab): void {
        if (tab !== this.activeTab() && (tab === 'cancellations' || this.activeTab() === 'cancellations')) {
            this.status.set('');
        }
        this.activeTab.set(tab);
        this.page.set(1);
        this.selectedMetadata.set(null);
        this.syncUrl();
    }

    public applyFilters(): void {
        this.page.set(1);
        this.selectedMetadata.set(null);
        this.syncUrl();
    }

    public setPeriod(range: AdminPeriod): void {
        this.periodRange = { from: range.from ?? '', to: range.to ?? '' };
        this.fromDate.set(this.periodRange.from);
        this.toDate.set(this.periodRange.to);
    }

    public resetFilters(): void {
        this.provider.set('');
        this.status.set('');
        this.kind.set('');
        this.search.set('');
        this.fromDate.set('');
        this.toDate.set('');
        this.page.set(1);
        this.selectedMetadata.set(null);
        this.syncUrl(true);
    }

    public goToPage(page: number): void {
        if (this.router.currentNavigation() !== null || this.isLoading() || page < 1 || page > this.totalPages()) {
            return;
        }

        this.page.set(page);
        this.syncUrl();
    }

    private syncUrl(clearPeriod = false): void {
        const preservePeriod = !clearPeriod && this.periodRange.from === this.fromDate() && this.periodRange.to === this.toDate();
        void this.router.navigate([], {
            relativeTo: this.route,
            queryParamsHandling: 'merge',
            queryParams: {
                tab: this.activeTab(),
                page: this.page(),
                search: adminQueryValue(this.search()),
                provider: adminQueryValue(this.provider()),
                status: adminQueryValue(this.status()),
                kind: adminQueryValue(this.kind()),
                ...(preservePeriod
                    ? {}
                    : {
                          period: this.fromDate().length > 0 && this.toDate().length > 0 ? 'custom' : 'all',
                          from: adminQueryValue(this.fromDate()),
                          to: adminQueryValue(this.toDate()),
                      }),
            },
        });
    }

    public showMetadata(value?: string | null): void {
        this.selectedMetadata.set(value !== null && value !== undefined && value.length > 0 ? this.formatJson(value) : null);
    }

    public load(): void {
        const tab = this.activeTab();
        this.requestedLoad.set({
            tab,
            page: this.page(),
            queryKey: this.queryKey(),
            filters: this.buildFilters(tab),
            revenueFilters: tab === 'cancellations' ? undefined : this.revenueFilters(),
        });
    }

    private queryKey(): string {
        const query = this.route.snapshot.queryParams;
        const values = BILLING_QUERY_FIELDS.map(field => {
            const value: unknown = query[field];
            const first: unknown = Array.isArray(value) ? value[0] : value;
            return typeof first === 'string' ? first : undefined;
        });
        return JSON.stringify(values);
    }

    public clearInvalidPeriod(): void {
        this.requestedLoad.set(undefined);
        this.selectedMetadata.set(null);
    }

    private loadRecords({ tab, page, filters }: BillingRequest): Observable<BillingRecords> {
        switch (tab) {
            case 'subscriptions': {
                return this.billingService.getSubscriptions(page, this.limit, filters).pipe(map(data => ({ tab, data })));
            }
            case 'payments': {
                return this.billingService.getPayments(page, this.limit, filters).pipe(map(data => ({ tab, data })));
            }
            case 'webhook-events': {
                return this.billingService.getWebhookEvents(page, this.limit, filters).pipe(map(data => ({ tab, data })));
            }
            case 'cancellations': {
                return this.billingService.getCancellations(page, this.limit, filters).pipe(map(data => ({ tab, data })));
            }
        }
    }

    private revenueFilters(): AdminBillingFilters {
        const from = this.fromDate();
        const to = this.toDate();
        const range = adminExclusiveDatePeriod({ from: from.length > 0 ? from : undefined, to: to.length > 0 ? to : undefined });
        return { fromUtc: `${range.from}T00:00:00Z`, toUtc: `${range.to}T00:00:00Z` };
    }

    private buildFilters(tab: AdminBillingTab): AdminBillingFilters {
        const optional = (value: string): string | null => {
            const trimmed = value.trim();
            return trimmed.length > 0 ? trimmed : null;
        };

        return {
            provider: optional(this.provider()),
            status: optional(this.status()),
            kind: tab === 'payments' ? optional(this.kind()) : null,
            search: optional(this.search()),
            fromUtc: this.toUtcStart(this.fromDate()),
            toUtc:
                tab === 'cancellations' && this.toDate().length > 0
                    ? `${adminExclusiveDatePeriod({ to: this.toDate() }).to}T00:00:00Z`
                    : this.toUtcEnd(this.toDate()),
        };
    }

    private formatMoney(amount?: number | null, currency?: string | null): string {
        if (amount === null || amount === undefined) {
            return '-';
        }

        const formattedAmount = amount.toFixed(PAYMENT_DECIMAL_PLACES).replace(/(\.\d{2})0$/u, '$1');
        return currency !== null && currency !== undefined && currency.length > 0 ? `${formattedAmount} ${currency}` : formattedAmount;
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

    private toUtcStart(value: string): string | null {
        return value.length > 0 ? new Date(`${value}T00:00:00.000Z`).toISOString() : null;
    }

    private toUtcEnd(value: string): string | null {
        // Keep the inclusive .NET day bound at tick precision for persisted rows.
        return value.length > 0 ? `${value}T23:59:59.9999999Z` : null;
    }

    private formatJson(value: string): string {
        try {
            return JSON.stringify(JSON.parse(value), null, 2);
        } catch {
            return value;
        }
    }
}
