import { InjectionToken } from '@angular/core';
import type { Observable } from 'rxjs';

import type { BillingOverview, PortalSessionResponse } from '../../../shared/models/billing.models';

export type BillingAccountActions = {
    getOverview: () => Observable<BillingOverview>;
    createPortalSession: () => Observable<PortalSessionResponse>;
};

export const BILLING_ACCOUNT_ACTIONS = new InjectionToken<BillingAccountActions>('BillingAccountActions');
