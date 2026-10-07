import { HttpClient } from '@angular/common/http';
import { inject, Service } from '@angular/core';
import { catchError, map, type Observable } from 'rxjs';

import { environment } from '../../../../environments/environment';
import { BillingSdk } from '../../../shared/api/sdk/generated/api/billing.service';
import type { BillingOverviewHttpResponse } from '../../../shared/api/sdk/generated/model/billing-overview-http-response';
import { createSdkConnection } from '../../../shared/api/sdk/sdk-connection';
import { requireSdkFields, sdkEnum } from '../../../shared/api/sdk/sdk-response';
import { rethrowApiError } from '../../../shared/lib/api-error.utils';
import type {
    BillingOverview,
    BillingPlan,
    BillingProvider,
    CheckoutSessionResponse,
    PortalSessionResponse,
} from '../../../shared/models/billing.models';

@Service()
export class PremiumBillingService {
    protected readonly baseUrl = environment.apiUrls.billing;
    private readonly sdk = createSdkConnection(BillingSdk, this.baseUrl, inject(HttpClient));

    public getOverview(): Observable<BillingOverview> {
        return this.sdk.client.getBillingOverview({ version: this.sdk.version }).pipe(
            map(billingOverviewFromSdk),
            catchError((error: unknown) => rethrowApiError('Billing overview error', error)),
        );
    }

    public startPremiumTrial(): Observable<BillingOverview> {
        return this.sdk.client.postBillingTrial({ version: this.sdk.version }).pipe(
            map(billingOverviewFromSdk),
            catchError((error: unknown) => rethrowApiError('Start premium trial error', error)),
        );
    }

    public createCheckoutSession(plan: BillingPlan, provider?: BillingProvider): Observable<CheckoutSessionResponse> {
        const payload = provider !== undefined ? { plan, provider } : { plan };
        return this.sdk.client
            .postBillingCheckoutSession({
                version: this.sdk.version,
                idempotencyKey: crypto.randomUUID(),
                createCheckoutSessionHttpRequest: payload,
            })
            .pipe(
                map(response => {
                    const value = requireSdkFields(response, ['sessionId', 'url', 'plan']);
                    return { ...value, plan: sdkEnum(value.plan, ['monthly', 'yearly'] as const) };
                }),
                catchError((error: unknown) => rethrowApiError('Create checkout session error', error)),
            );
    }

    public createPortalSession(): Observable<PortalSessionResponse> {
        return this.sdk.client.postBillingPortalSession({ version: this.sdk.version }).pipe(
            map(response => requireSdkFields(response, ['url'])),
            catchError((error: unknown) => rethrowApiError('Create portal session error', error)),
        );
    }
}

function billingOverviewFromSdk(response: BillingOverviewHttpResponse): BillingOverview {
    const value = requireSdkFields(response, [
        'isPremium',
        'cancelAtPeriodEnd',
        'renewalEnabled',
        'manageBillingAvailable',
        'premiumTrialActive',
        'premiumTrialUsed',
        'canStartPremiumTrial',
        'provider',
        'availableProviders',
    ]);
    return {
        ...value,
        plan: billingPlanFromSdk(value.plan),
        subscriptionStatus: value.subscriptionStatus ?? null,
        subscriptionProvider: value.subscriptionProvider ?? null,
        currentPeriodStartUtc: value.currentPeriodStartUtc ?? null,
        currentPeriodEndUtc: value.currentPeriodEndUtc ?? null,
        nextBillingAttemptUtc: value.nextBillingAttemptUtc ?? null,
        premiumTrialStartUtc: value.premiumTrialStartUtc ?? null,
        premiumTrialEndUtc: value.premiumTrialEndUtc ?? null,
        paddleClientToken: value.paddleClientToken ?? null,
    };
}

function billingPlanFromSdk(value: string | null | undefined): BillingPlan | null {
    return value === null || value === undefined ? null : sdkEnum(value, ['monthly', 'yearly'] as const);
}
