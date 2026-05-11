import { CommonModule, formatDate } from '@angular/common';
import { ChangeDetectionStrategy, Component, computed, DestroyRef, inject, LOCALE_ID, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormsModule } from '@angular/forms';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button.component';
import { FdUiInputComponent } from 'fd-ui-kit/input/fd-ui-input.component';

import {
    type AdminBillingFilters,
    type AdminBillingPayment,
    AdminBillingService,
    type AdminBillingSubscription,
    type AdminBillingTab,
    type AdminBillingWebhookEvent,
    type PagedResponse,
} from '../api/admin-billing.service';

interface AdminBillingSubscriptionViewModel extends AdminBillingSubscription {
    currentPeriodStartText: string;
    currentPeriodEndText: string;
    nextBillingAttemptText: string;
    updatedText: string;
    externalCustomerIdText: string;
    externalSubscriptionIdText: string;
    externalPaymentMethodIdText: string;
}

interface AdminBillingPaymentViewModel extends AdminBillingPayment {
    createdText: string;
    amountText: string;
    externalPaymentIdText: string;
    externalCustomerIdText: string;
    webhookEventIdText: string;
}

interface AdminBillingWebhookEventViewModel extends AdminBillingWebhookEvent {
    processedText: string;
    eventIdText: string;
    externalObjectIdText: string;
}

const DEFAULT_PAGE_SIZE = 20;
const SHORT_ID_MIN_LENGTH = 18;
const SHORT_ID_PREFIX_LENGTH = 8;
const SHORT_ID_SUFFIX_START = -6;

@Component({
    selector: 'fd-admin-billing',
    standalone: true,
    imports: [CommonModule, FormsModule, FdUiButtonComponent, FdUiInputComponent],
    templateUrl: './admin-billing.component.html',
    styleUrl: './admin-billing.component.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AdminBillingComponent {
    private readonly billingService = inject(AdminBillingService);
    private readonly destroyRef = inject(DestroyRef);
    private readonly locale = inject(LOCALE_ID);

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
    public readonly provider = signal('');
    public readonly status = signal('');
    public readonly kind = signal('');
    public readonly search = signal('');
    public readonly fromDate = signal('');
    public readonly toDate = signal('');

    public constructor() {
        this.load();
    }

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

    public load(): void {
        this.isLoading.set(true);
        const filters = this.buildFilters();

        if (this.activeTab() === 'subscriptions') {
            this.billingService
                .getSubscriptions(this.page(), this.limit, filters)
                .pipe(takeUntilDestroyed(this.destroyRef))
                .subscribe({
                    next: response => {
                        this.subscriptions.set(response.items);
                        this.applyPageData(response);
                    },
                    error: () => {
                        this.clearData();
                    },
                });
            return;
        }

        if (this.activeTab() === 'payments') {
            this.billingService
                .getPayments(this.page(), this.limit, filters)
                .pipe(takeUntilDestroyed(this.destroyRef))
                .subscribe({
                    next: response => {
                        this.payments.set(response.items);
                        this.applyPageData(response);
                    },
                    error: () => {
                        this.clearData();
                    },
                });
            return;
        }

        this.billingService
            .getWebhookEvents(this.page(), this.limit, filters)
            .pipe(takeUntilDestroyed(this.destroyRef))
            .subscribe({
                next: response => {
                    this.webhookEvents.set(response.items);
                    this.applyPageData(response);
                },
                error: () => {
                    this.clearData();
                },
            });
    }

    private applyPageData<T>(response: PagedResponse<T>): void {
        this.totalPages.set(response.totalPages > 0 ? response.totalPages : 1);
        this.totalItems.set(response.totalItems);
        this.isLoading.set(false);
    }

    private clearData(): void {
        this.subscriptions.set([]);
        this.payments.set([]);
        this.webhookEvents.set([]);
        this.totalPages.set(1);
        this.totalItems.set(0);
        this.isLoading.set(false);
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
