import { CommonModule, DatePipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, DestroyRef, inject, signal } from '@angular/core';
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

@Component({
    selector: 'fd-admin-billing',
    standalone: true,
    imports: [CommonModule, FormsModule, DatePipe, FdUiButtonComponent, FdUiInputComponent],
    templateUrl: './admin-billing.component.html',
    styleUrl: './admin-billing.component.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AdminBillingComponent {
    private readonly billingService = inject(AdminBillingService);
    private readonly destroyRef = inject(DestroyRef);

    public readonly activeTab = signal<AdminBillingTab>('subscriptions');
    public readonly subscriptions = signal<AdminBillingSubscription[]>([]);
    public readonly payments = signal<AdminBillingPayment[]>([]);
    public readonly webhookEvents = signal<AdminBillingWebhookEvent[]>([]);
    public readonly selectedMetadata = signal<string | null>(null);
    public readonly totalPages = signal(1);
    public readonly totalItems = signal(0);
    public readonly page = signal(1);
    public readonly limit = 20;
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
        this.selectedMetadata.set(value ? this.formatJson(value) : null);
    }

    public formatMoney(amount?: number | null, currency?: string | null): string {
        if (amount === null || amount === undefined) {
            return '-';
        }

        return currency ? `${amount.toFixed(2)} ${currency}` : amount.toFixed(2);
    }

    public shortId(value?: string | null): string {
        if (!value) {
            return '-';
        }

        return value.length > 18 ? `${value.slice(0, 8)}...${value.slice(-6)}` : value;
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
        this.totalPages.set(response.totalPages || 1);
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
        return {
            provider: this.provider().trim() || null,
            status: this.status().trim() || null,
            kind: this.activeTab() === 'payments' ? this.kind().trim() || null : null,
            search: this.search().trim() || null,
            fromUtc: this.toUtcStart(this.fromDate()),
            toUtc: this.toUtcEnd(this.toDate()),
        };
    }

    private toUtcStart(value: string): string | null {
        return value ? new Date(`${value}T00:00:00.000Z`).toISOString() : null;
    }

    private toUtcEnd(value: string): string | null {
        return value ? new Date(`${value}T23:59:59.999Z`).toISOString() : null;
    }

    private formatJson(value: string): string {
        try {
            return JSON.stringify(JSON.parse(value), null, 2);
        } catch {
            return value;
        }
    }
}
