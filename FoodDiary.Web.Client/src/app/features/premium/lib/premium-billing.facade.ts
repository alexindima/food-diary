import { inject, Injectable } from '@angular/core';
import type { Observable } from 'rxjs';

import { PremiumBillingService } from '../api/premium-billing.service';
import type {
    BillingOverview,
    BillingPlan,
    BillingProvider,
    CheckoutSessionResponse,
    PortalSessionResponse,
} from '../models/billing.models';

@Injectable({ providedIn: 'root' })
export class PremiumBillingFacade {
    private readonly billingService = inject(PremiumBillingService);

    public getOverview(): Observable<BillingOverview> {
        return this.billingService.getOverview();
    }

    public startPremiumTrial(): Observable<BillingOverview> {
        return this.billingService.startPremiumTrial();
    }

    public createCheckoutSession(plan: BillingPlan, provider?: BillingProvider): Observable<CheckoutSessionResponse> {
        return this.billingService.createCheckoutSession(plan, provider);
    }

    public createPortalSession(): Observable<PortalSessionResponse> {
        return this.billingService.createPortalSession();
    }
}
