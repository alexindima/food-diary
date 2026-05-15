import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { TranslateModule } from '@ngx-translate/core';
import { describe, expect, it } from 'vitest';

import type { PremiumPlanCardViewModel } from '../../premium-access/premium-access-lib/premium-access.types';
import { PremiumPlansCardComponent } from './premium-plans-card.component';

describe('PremiumPlansCardComponent', () => {
    it('shows provider choices when at least one card has multiple providers', () => {
        const { component, fixture } = setupComponent([createPlanCard({ providerCount: 2 })]);
        const host = fixture.nativeElement as HTMLElement;

        expect(component.showProviderChoices()).toBe(true);
        expect(host.textContent).toContain('PREMIUM_PAGE.PLANS.PAY_WITH');
    });

    it('disables checkout actions while any plan card is loading', () => {
        const { component } = setupComponent([
            createPlanCard({ plan: 'monthly', isLoading: false }),
            createPlanCard({ plan: 'yearly', isLoading: true }),
        ]);

        expect(component.checkoutDisabled()).toBe(true);
    });
});

function setupComponent(cards: PremiumPlanCardViewModel[]): {
    component: PremiumPlansCardComponent;
    fixture: ComponentFixture<PremiumPlansCardComponent>;
} {
    TestBed.configureTestingModule({
        imports: [PremiumPlansCardComponent, TranslateModule.forRoot()],
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
