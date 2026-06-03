import { HttpClient, HttpParams } from '@angular/common/http';
import { inject, Service } from '@angular/core';
import { map, type Observable } from 'rxjs';

import { environment } from '../../../../environments/environment';
import type {
    AdminBillingFilters,
    AdminBillingPayment,
    AdminBillingSubscription,
    AdminBillingTab,
    AdminBillingWebhookEvent,
    PagedResponse,
} from '../models/admin-billing.models';

type ApiPagedResponse<T> = {
    data: T[];
    page: number;
    limit: number;
    totalPages: number;
    totalItems: number;
};

@Service()
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
