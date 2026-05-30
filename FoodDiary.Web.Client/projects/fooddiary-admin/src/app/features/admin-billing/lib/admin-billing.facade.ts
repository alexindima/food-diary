import { inject, Injectable } from '@angular/core';
import type { Observable } from 'rxjs';

import { AdminBillingService } from '../api/admin-billing.service';
import type {
    AdminBillingFilters,
    AdminBillingPayment,
    AdminBillingSubscription,
    AdminBillingWebhookEvent,
    PagedResponse,
} from '../models/admin-billing.models';

@Injectable({ providedIn: 'root' })
export class AdminBillingFacade {
    private readonly billingService = inject(AdminBillingService);

    public getSubscriptions(
        page: number,
        limit: number,
        filters: AdminBillingFilters,
    ): Observable<PagedResponse<AdminBillingSubscription>> {
        return this.billingService.getSubscriptions(page, limit, filters);
    }

    public getPayments(page: number, limit: number, filters: AdminBillingFilters): Observable<PagedResponse<AdminBillingPayment>> {
        return this.billingService.getPayments(page, limit, filters);
    }

    public getWebhookEvents(
        page: number,
        limit: number,
        filters: AdminBillingFilters,
    ): Observable<PagedResponse<AdminBillingWebhookEvent>> {
        return this.billingService.getWebhookEvents(page, limit, filters);
    }
}
