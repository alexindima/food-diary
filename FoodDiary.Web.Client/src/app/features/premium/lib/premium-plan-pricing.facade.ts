import { isPlatformBrowser } from '@angular/common';
import { DestroyRef, inject, Injectable, PLATFORM_ID, signal } from '@angular/core';

import type { BillingOverview } from '../../../shared/models/billing.models';
import type { PremiumPlanPrices } from '../models/premium-plan-price';
import { PaddleCheckoutService, type PaddleEnvironment } from './paddle-checkout.service';

@Injectable()
export class PremiumPlanPricingFacade {
    private readonly paddle = inject(PaddleCheckoutService);
    private readonly destroyRef = inject(DestroyRef);
    private readonly isBrowser = isPlatformBrowser(inject(PLATFORM_ID));
    private requestVersion = 0;

    public readonly prices = signal<PremiumPlanPrices>({});
    public readonly loading = signal(false);
    public readonly unavailable = signal(false);

    public async loadPricesAsync(overview: BillingOverview, locale: string): Promise<void> {
        const requestVersion = ++this.requestVersion;
        this.prices.set({});
        this.unavailable.set(false);
        this.loading.set(false);
        if (
            !this.isBrowser ||
            this.destroyRef.destroyed ||
            !overview.availableProviders.some(provider => provider.toLowerCase() === 'paddle')
        ) {
            return;
        }

        const configuration = resolvePriceConfiguration(overview);
        if (configuration === null) {
            this.unavailable.set(true);
            return;
        }

        this.loading.set(true);
        try {
            const prices = await this.paddle.previewPlanPricesAsync(
                { monthly: configuration.monthly, yearly: configuration.yearly },
                {
                    token: configuration.token,
                    environment: configuration.environment,
                    locale,
                },
            );
            if (this.isCurrentRequest(requestVersion)) {
                this.prices.set(prices);
            }
        } catch {
            if (this.isCurrentRequest(requestVersion)) {
                this.unavailable.set(true);
            }
        } finally {
            if (this.isCurrentRequest(requestVersion)) {
                this.loading.set(false);
            }
        }
    }

    private isCurrentRequest(requestVersion: number): boolean {
        return !this.destroyRef.destroyed && this.requestVersion === requestVersion;
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
