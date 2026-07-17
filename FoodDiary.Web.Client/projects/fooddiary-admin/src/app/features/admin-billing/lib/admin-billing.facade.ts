import { formatDate } from '@angular/common';
import { computed, DestroyRef, inject, Injectable, LOCALE_ID, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';

import { AdminBillingService } from '../api/admin-billing.service';
import type {
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
} from '../pages/admin-billing.types';

const DEFAULT_PAGE_SIZE = 20;
const SHORT_ID_MIN_LENGTH = 18;
const SHORT_ID_PREFIX_LENGTH = 8;
const SHORT_ID_SUFFIX_START = -6;

@Injectable()
export class AdminBillingFacade {
    private readonly billingService = inject(AdminBillingService);
    private readonly destroyRef = inject(DestroyRef);
    private readonly locale = inject(LOCALE_ID);
    private loadRequestId = 0;

    public readonly activeTab = signal<AdminBillingTab>('subscriptions');
    public readonly subscriptions = signal<AdminBillingSubscription[]>([]);
    public readonly payments = signal<AdminBillingPayment[]>([]);
    public readonly webhookEvents = signal<AdminBillingWebhookEvent[]>([]);
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
            processedText: this.formatDateLabel(event.processedAtUtc),
            eventIdText: this.shortId(event.eventId),
            externalObjectIdText: this.shortId(event.externalObjectId),
        })),
    );
    public readonly selectedMetadata = signal<string | null>(null);
    public readonly totalPages = signal(1);
    public readonly totalItems = signal(0);
    public readonly page = signal(1);
    public readonly limit = DEFAULT_PAGE_SIZE;
    public readonly isLoading = signal(false);
    public readonly errorMessage = signal<string | null>(null);
    public readonly provider = signal('');
    public readonly status = signal('');
    public readonly kind = signal('');
    public readonly search = signal('');
    public readonly fromDate = signal('');
    public readonly toDate = signal('');

    public setTab(tab: AdminBillingTab): void {
        this.activeTab.set(tab);
        this.page.set(1);
        this.selectedMetadata.set(null);
        this.load();
    }

    public applyFilters(): void {
        this.page.set(1);
        this.selectedMetadata.set(null);
        this.load();
    }

    public resetFilters(): void {
        this.provider.set('');
        this.status.set('');
        this.kind.set('');
        this.search.set('');
        this.fromDate.set('');
        this.toDate.set('');
        this.applyFilters();
    }

    public goToPage(page: number): void {
        if (page < 1 || page > this.totalPages()) {
            return;
        }

        this.page.set(page);
        this.load();
    }

    public showMetadata(value?: string | null): void {
        this.selectedMetadata.set(value !== null && value !== undefined && value.length > 0 ? this.formatJson(value) : null);
    }

    public load(): void {
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
                        this.applySubscriptions(response, requestId, tab);
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
                        this.applyPayments(response, requestId, tab);
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
                    this.applyWebhookEvents(response, requestId, tab);
                },
                error: (error: unknown) => {
                    this.applyLoadError(requestId, tab, error);
                },
            });
    }

    private applySubscriptions(response: PagedResponse<AdminBillingSubscription>, requestId: number, tab: AdminBillingTab): void {
        if (this.isCurrentLoad(requestId, tab)) {
            this.subscriptions.set(response.items);
            this.applyPageData(response);
        }
    }

    private applyPayments(response: PagedResponse<AdminBillingPayment>, requestId: number, tab: AdminBillingTab): void {
        if (this.isCurrentLoad(requestId, tab)) {
            this.payments.set(response.items);
            this.applyPageData(response);
        }
    }

    private applyWebhookEvents(response: PagedResponse<AdminBillingWebhookEvent>, requestId: number, tab: AdminBillingTab): void {
        if (this.isCurrentLoad(requestId, tab)) {
            this.webhookEvents.set(response.items);
            this.applyPageData(response);
        }
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

        this.errorMessage.set(error instanceof Error && error.message.length > 0 ? error.message : 'Failed to load billing records.');
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

    private buildFilters(): AdminBillingFilters {
        const optional = (value: string): string | null => {
            const trimmed = value.trim();
            return trimmed.length > 0 ? trimmed : null;
        };

        return {
            provider: optional(this.provider()),
            status: optional(this.status()),
            kind: this.activeTab() === 'payments' ? optional(this.kind()) : null,
            search: optional(this.search()),
            fromUtc: this.toUtcStart(this.fromDate()),
            toUtc: this.toUtcEnd(this.toDate()),
        };
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
