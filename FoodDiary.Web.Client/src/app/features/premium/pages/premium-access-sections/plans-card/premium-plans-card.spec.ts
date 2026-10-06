import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { describe, expect, it, vi } from 'vitest';

import { provideTranslateTesting } from '../../../../../../testing/translate-testing.module';
import type { PremiumPlanCardViewModel } from '../../premium-access/premium-access-lib/premium-access.types';
import { PremiumPlansCardComponent } from './premium-plans-card';

describe('PremiumPlansCardComponent', () => {
    it('shows provider choices when at least one card has multiple providers', () => {
        const { component, fixture } = setupComponent([createPlanCard({ providerCount: 2 })]);
        const host = fixture.nativeElement as HTMLElement;

        expect(component['showProviderChoices']()).toBe(true);
        expect(host.textContent).toContain('PREMIUM_PAGE.PLANS.PAY_WITH');
    });

    it('disables checkout actions while any plan card is loading', () => {
        const { component } = setupComponent([
            createPlanCard({ plan: 'monthly', isLoading: false }),
            createPlanCard({ plan: 'yearly', isLoading: true }),
        ]);

        expect(component['checkoutDisabled']()).toBe(true);
    });

    it('shows a real formatted amount, explicit currency and correct recurring period for both plans', () => {
        const { fixture } = setupComponent([createPlanCard({ plan: 'monthly' }), createPlanCard({ plan: 'yearly' })]);
        fixture.componentRef.setInput('prices', {
            monthly: { formattedTotal: '€9.49', currencyCode: 'EUR' },
            yearly: { formattedTotal: '€79.90', currencyCode: 'EUR' },
        });
        fixture.detectChanges();
        const prices = (fixture.nativeElement as HTMLElement).querySelectorAll('fd-premium-plan-price');
        expect(prices[0].textContent).toContain('€9.49');
        expect(prices[0].textContent).toContain('EUR PREMIUM_PAGE.PLANS.PRICE_MONTHLY_PERIOD');
        expect(prices[1].textContent).toContain('€79.90');
        expect(prices[1].textContent).toContain('EUR PREMIUM_PAGE.PLANS.PRICE_YEARLY_PERIOD');
    });

    it('announces loading without inventing a price and offers retry when prices are unavailable', () => {
        const { component, fixture } = setupComponent([createPlanCard()]);
        fixture.componentRef.setInput('pricesLoading', true);
        fixture.detectChanges();
        const host = fixture.nativeElement as HTMLElement;
        expect(host.querySelector('[role="status"]')?.getAttribute('aria-busy')).toBe('true');
        expect(host.textContent).toContain('PREMIUM_PAGE.PLANS.PRICE_LOADING');
        expect(host.querySelector('strong')).toBeNull();

        fixture.componentRef.setInput('pricesLoading', false);
        fixture.componentRef.setInput('pricesUnavailable', true);
        fixture.detectChanges();
        const retry = vi.fn();
        component.retryPrices.subscribe(retry);
        Array.from(host.querySelectorAll<HTMLButtonElement>('button'))
            .find(button => button.textContent.includes('PREMIUM_PAGE.PLANS.PRICE_RETRY'))
            ?.click();
        expect(host.textContent).toContain('PREMIUM_PAGE.PLANS.PRICE_UNAVAILABLE');
        expect(retry).toHaveBeenCalledOnce();
    });
});

function setupComponent(cards: PremiumPlanCardViewModel[]): {
    component: PremiumPlansCardComponent;
    fixture: ComponentFixture<PremiumPlansCardComponent>;
} {
    TestBed.configureTestingModule({
        imports: [PremiumPlansCardComponent],
        providers: [provideTranslateTesting()],
    });

    const fixture = TestBed.createComponent(PremiumPlansCardComponent);
    fixture.componentRef.setInput('cards', cards);
    fixture.detectChanges();

    return {
        component: fixture.componentInstance,
        fixture,
    };
}

function createPlanCard(
    options: { plan?: 'monthly' | 'yearly'; providerCount?: number; isLoading?: boolean } = {},
): PremiumPlanCardViewModel {
    const providerCount = options.providerCount ?? 1;

    return {
        plan: options.plan ?? 'monthly',
        titleKey: 'PREMIUM_PAGE.PLANS.MONTHLY_TITLE',
        descriptionKey: 'PREMIUM_PAGE.PLANS.MONTHLY_DESCRIPTION',
        actionKey: 'PREMIUM_PAGE.PLANS.MONTHLY_ACTION',
        kickerKey: null,
        isFeatured: false,
        isLoading: options.isLoading ?? false,
        providerOptions: Array.from({ length: providerCount }, (_, index) => ({
            provider: `provider-${index + 1}`,
            label: `Provider ${index + 1}`,
        })),
    };
}
