import { HttpErrorResponse } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { catchError, Observable } from 'rxjs';

import { environment } from '../../../../environments/environment';
import { ApiService } from '../../../services/api.service';
import { rethrowApiError } from '../../../shared/lib/api-error.utils';
import { BillingOverview, BillingPlan, BillingProvider, CheckoutSessionResponse, PortalSessionResponse } from '../models/billing.models';

@Injectable({
    providedIn: 'root',
})
export class PremiumBillingService extends ApiService {
    protected readonly baseUrl = environment.apiUrls.billing;

    public getOverview(): Observable<BillingOverview> {
        return this.get<BillingOverview>('overview').pipe(
            catchError((error: HttpErrorResponse) => rethrowApiError('Billing overview error', error)),
        );
    }

    public createCheckoutSession(plan: BillingPlan, provider?: BillingProvider): Observable<CheckoutSessionResponse> {
        const payload = provider ? { plan, provider } : { plan };
        return this.post<CheckoutSessionResponse>('checkout-session', payload).pipe(
            catchError((error: HttpErrorResponse) => rethrowApiError('Create checkout session error', error)),
        );
    }

    public createPortalSession(): Observable<PortalSessionResponse> {
        return this.post<PortalSessionResponse>('portal-session', {}).pipe(
            catchError((error: HttpErrorResponse) => rethrowApiError('Create portal session error', error)),
        );
    }
}
