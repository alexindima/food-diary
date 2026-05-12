import { Injectable } from '@angular/core';
import { catchError, type Observable } from 'rxjs';

import { environment } from '../../../../environments/environment';
import { ApiService } from '../../../services/api.service';
import { rethrowApiError } from '../../../shared/lib/api-error.utils';
import type {
    BillingOverview,
    BillingPlan,
    BillingProvider,
    CheckoutSessionResponse,
    PortalSessionResponse,
} from '../models/billing.models';

@Injectable({
    providedIn: 'root',
})
export class PremiumBillingService extends ApiService {
    protected readonly baseUrl = environment.apiUrls.billing;

    public getOverview(): Observable<BillingOverview> {
        return this.get<BillingOverview>('overview').pipe(catchError((error: unknown) => rethrowApiError('Billing overview error', error)));
    }

    public createCheckoutSession(plan: BillingPlan, provider?: BillingProvider): Observable<CheckoutSessionResponse> {
        const payload = provider !== undefined ? { plan, provider } : { plan };
        return this.post<CheckoutSessionResponse>('checkout-session', payload).pipe(
            catchError((error: unknown) => rethrowApiError('Create checkout session error', error)),
        );
    }

    public createPortalSession(): Observable<PortalSessionResponse> {
        return this.post<PortalSessionResponse>('portal-session', {}).pipe(
            catchError((error: unknown) => rethrowApiError('Create portal session error', error)),
        );
    }
}
