import { isPlatformBrowser } from '@angular/common';
import { computed, DestroyRef, inject, Injectable, PLATFORM_ID, resource, signal } from '@angular/core';

import type { BillingOverview } from '../../../shared/models/billing.models';
import type { PremiumPlanPrices } from '../models/premium-plan-price';
import { PaddleCheckoutService, type PaddleEnvironment } from './paddle-checkout.service';

@Injectable()
export class PremiumPlanPricingFacade {
    private readonly paddle = inject(PaddleCheckoutService);
    private readonly destroyRef = inject(DestroyRef);
    private readonly isBrowser = isPlatformBrowser(inject(PLATFORM_ID));
    private readonly requestedPreview = signal<{ result: Promise<PremiumPlanPrices | null> } | null | undefined>(undefined);
    private readonly preview = resource({
        params: () => this.requestedPreview() ?? undefined,
        loader: async ({ params }) => params.result,
    });

    public readonly prices = computed<PremiumPlanPrices>(() => this.preview.value() ?? {});
    public readonly loading = this.preview.isLoading;
    public readonly unavailable = computed(() => this.requestedPreview() === null || this.preview.value() === null);

    public loadPrices(overview: BillingOverview, locale: string): boolean {
        if (
            !this.isBrowser ||
            this.destroyRef.destroyed ||
            !overview.availableProviders.some(provider => provider.toLowerCase() === 'paddle')
        ) {
            this.requestedPreview.set(undefined);
            return false;
        }

        const configuration = resolvePriceConfiguration(overview);
        if (configuration === null) {
            this.requestedPreview.set(null);
            return false;
        }

        const { monthly, yearly, token, environment } = configuration;
        const result = this.paddle.previewPlanPricesAsync({ monthly, yearly }, { token, environment, locale });
        // A replacement can arrive before the resource observes this promise.
        this.requestedPreview.set({ result: result.catch(() => null) });
        return true;
    }
}

function resolvePriceConfiguration(
    overview: BillingOverview,
): { token: string; monthly: string; yearly: string; environment: PaddleEnvironment } | null {
    const token = trimConfigValue(overview.paddleClientToken);
    const monthly = trimConfigValue(overview.paddleMonthlyPriceId);
    const yearly = trimConfigValue(overview.paddleYearlyPriceId);
    if (monthly === yearly || token.length === 0 || monthly.length === 0 || yearly.length === 0) {
        return null;
    }
    return { token, monthly, yearly, environment: token.startsWith('test_') ? 'sandbox' : 'production' };
}

function trimConfigValue(value: string | null | undefined): string {
    return value?.trim() ?? '';
}
