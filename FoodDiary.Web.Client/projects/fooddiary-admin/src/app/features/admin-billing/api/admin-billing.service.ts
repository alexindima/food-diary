import { HttpClient, HttpParams } from '@angular/common/http';
import { inject, Service } from '@angular/core';
import { map, type Observable } from 'rxjs';

import { environment } from '../../../../environments/environment';
import { AdminBillingSdk } from '../../../shared/api/sdk/generated/api/admin-billing.service';
import { createSdkConnection, sdkRequestOptions } from '../../../shared/api/sdk/sdk-connection';
import { sdkItemsPage } from '../../../shared/api/sdk/sdk-response';
import type {
    AdminBillingFilters,
    AdminBillingPayment,
    AdminBillingRevenueSummary,
    AdminBillingSubscription,
    AdminBillingWebhookEvent,
    PagedResponse,
} from '../models/admin-billing.models';
import {
    adminBillingPaymentFromSdk,
    adminBillingRevenueSummaryFromSdk,
    adminBillingSubscriptionFromSdk,
    adminBillingWebhookEventFromSdk,
} from './admin-billing-sdk.mapper';

@Service()
export class AdminBillingService {
    private readonly http = inject(HttpClient);
    private readonly baseUrl = `${environment.apiUrls.auth.replace(/\/auth$/, '')}/admin/billing`;
    private readonly sdk = createSdkConnection(AdminBillingSdk, this.baseUrl, this.http);

    public getSubscriptions(
        page: number,
        limit: number,
        filters: AdminBillingFilters,
    ): Observable<PagedResponse<AdminBillingSubscription>> {
        return this.sdk.client
            .getAdminBillingSubscriptions(
                { version: this.sdk.version, page, limit },
                'body',
                false,
                sdkRequestOptions(undefined, this.buildFilterParams(filters)),
            )
            .pipe(map(response => sdkItemsPage(response, adminBillingSubscriptionFromSdk)));
    }

    public getPayments(page: number, limit: number, filters: AdminBillingFilters): Observable<PagedResponse<AdminBillingPayment>> {
        return this.sdk.client
            .getAdminBillingPayments(
                { version: this.sdk.version, page, limit },
                'body',
                false,
                sdkRequestOptions(undefined, this.buildFilterParams(filters)),
            )
            .pipe(map(response => sdkItemsPage(response, adminBillingPaymentFromSdk)));
    }

    public getRevenueSummary(filters: AdminBillingFilters): Observable<AdminBillingRevenueSummary> {
        return this.sdk.client
            .getAdminBillingRevenueSummary(
                { version: this.sdk.version },
                'body',
                false,
                sdkRequestOptions(undefined, this.buildFilterParams(filters)),
            )
            .pipe(map(adminBillingRevenueSummaryFromSdk));
    }

    public getWebhookEvents(
        page: number,
        limit: number,
        filters: AdminBillingFilters,
    ): Observable<PagedResponse<AdminBillingWebhookEvent>> {
        return this.sdk.client
            .getAdminBillingWebhookEvents(
                { version: this.sdk.version, page, limit },
                'body',
                false,
                sdkRequestOptions(undefined, this.buildFilterParams(filters)),
            )
            .pipe(map(response => sdkItemsPage(response, adminBillingWebhookEventFromSdk)));
    }

    private buildFilterParams(filters: AdminBillingFilters): HttpParams {
        let params = new HttpParams();

        for (const [key, value] of Object.entries(filters)) {
            if (typeof value === 'string' && value.trim().length > 0) {
                params = params.set(key, value);
            }
        }

        return params;
    }
}
