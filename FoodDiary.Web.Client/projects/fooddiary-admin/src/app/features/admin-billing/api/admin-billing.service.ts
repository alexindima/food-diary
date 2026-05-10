import { HttpClient, HttpParams } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { map, type Observable } from 'rxjs';

import { environment } from '../../../../environments/environment';

export type AdminBillingTab = 'subscriptions' | 'payments' | 'webhook-events';

export type AdminBillingFilters = {
    provider?: string | null;
    status?: string | null;
    kind?: string | null;
    search?: string | null;
    fromUtc?: string | null;
    toUtc?: string | null;
};

export type AdminBillingSubscription = {
    id: string;
    userId: string;
    userEmail: string;
    provider: string;
    externalCustomerId: string;
    externalSubscriptionId?: string | null;
    externalPaymentMethodId?: string | null;
    externalPriceId?: string | null;
    plan?: string | null;
    status: string;
    currentPeriodStartUtc?: string | null;
    currentPeriodEndUtc?: string | null;
    cancelAtPeriodEnd: boolean;
    nextBillingAttemptUtc?: string | null;
    lastWebhookEventId?: string | null;
    lastSyncedAtUtc?: string | null;
    createdOnUtc: string;
    modifiedOnUtc?: string | null;
};

export type AdminBillingPayment = {
    id: string;
    userId: string;
    userEmail: string;
    billingSubscriptionId?: string | null;
    provider: string;
    externalPaymentId: string;
    externalCustomerId?: string | null;
    externalSubscriptionId?: string | null;
    externalPaymentMethodId?: string | null;
    externalPriceId?: string | null;
    plan?: string | null;
    status: string;
    kind: string;
    amount?: number | null;
    currency?: string | null;
    currentPeriodStartUtc?: string | null;
    currentPeriodEndUtc?: string | null;
    webhookEventId?: string | null;
    providerMetadataJson?: string | null;
    createdOnUtc: string;
    modifiedOnUtc?: string | null;
};

export type AdminBillingWebhookEvent = {
    id: string;
    provider: string;
    eventId: string;
    eventType: string;
    externalObjectId?: string | null;
    status: string;
    processedAtUtc: string;
    payloadJson?: string | null;
    errorMessage?: string | null;
    createdOnUtc: string;
    modifiedOnUtc?: string | null;
};

type ApiPagedResponse<T> = {
    data: T[];
    page: number;
    limit: number;
    totalPages: number;
    totalItems: number;
};

export type PagedResponse<T> = {
    items: T[];
    page: number;
    limit: number;
    totalPages: number;
    totalItems: number;
};

@Injectable({ providedIn: 'root' })
export class AdminBillingService {
    private readonly http = inject(HttpClient);
    private readonly baseUrl = `${environment.apiUrls.auth.replace(/\/auth$/, '')}/admin/billing`;

    public getSubscriptions(
        page: number,
        limit: number,
        filters: AdminBillingFilters,
    ): Observable<PagedResponse<AdminBillingSubscription>> {
        return this.getPaged<AdminBillingSubscription>('subscriptions', page, limit, filters);
    }

    public getPayments(page: number, limit: number, filters: AdminBillingFilters): Observable<PagedResponse<AdminBillingPayment>> {
        return this.getPaged<AdminBillingPayment>('payments', page, limit, filters);
    }

    public getWebhookEvents(
        page: number,
        limit: number,
        filters: AdminBillingFilters,
    ): Observable<PagedResponse<AdminBillingWebhookEvent>> {
        return this.getPaged<AdminBillingWebhookEvent>('webhook-events', page, limit, filters);
    }

    private getPaged<T>(path: AdminBillingTab, page: number, limit: number, filters: AdminBillingFilters): Observable<PagedResponse<T>> {
        let params = new HttpParams().set('page', page).set('limit', limit);

        for (const [key, value] of Object.entries(filters)) {
            if (typeof value === 'string' && value.trim().length > 0) {
                params = params.set(key, value);
            }
        }

        return this.http.get<ApiPagedResponse<T>>(`${this.baseUrl}/${path}`, { params }).pipe(
            map(response => ({
                items: response.data,
                page: response.page,
                limit: response.limit,
                totalPages: response.totalPages,
                totalItems: response.totalItems,
            })),
        );
    }
}
