import type { ComponentFixture } from '@angular/core/testing';
import { TestBed } from '@angular/core/testing';
import { TranslateService } from '@ngx-translate/core';
import { beforeEach, describe, expect, it, vi } from 'vitest';

const TEST_PLAN_DURATION_DAYS = 7;

import { provideTranslateTesting } from '../../../../../../../testing/translate-testing.module';
import { entityId } from '../../../../../../shared/models/semantics/entity-id';
import type { MealPlanCardViewModel } from '../../../../lib/meal-plan-view.mapper';
import { planDurationDaysFromStored } from '../../../../models/meal-plan-values';
import { MealPlanListContentComponent } from './meal-plan-list-content';

describe('MealPlanListContentComponent', () => {
    beforeEach(() => {
        TestBed.configureTestingModule({
            imports: [MealPlanListContentComponent],
            providers: [provideTranslateTesting()],
        });
    });

    it('renders empty state when there are no plans', () => {
        const fixture = createComponent({ plans: [] });
        const element = getElement(fixture);

        expect(element.querySelector('.meal-plans-list__empty')).not.toBeNull();
        expect(element.querySelector('.meal-plan-card')).toBeNull();
    });

    it.each([true, false])('distinguishes a card with isCurated=%s and emits its opened plan id', isCurated => {
        const fixture = createComponent({ plans: [{ ...createPlanCard(), isCurated }] });
        const element = getElement(fixture);
        const planOpen = vi.fn();
        fixture.componentInstance['planOpen'].subscribe(planOpen);

        element.querySelector<HTMLElement>('.meal-plan-card')?.click();

        expect(element.querySelector('.meal-plan-card__name')?.textContent).toContain('Keto plan');
        expect(element.textContent).toContain(isCurated ? 'MEAL_PLANS.CURATED' : 'MEAL_PLANS.MY_PLAN');
        expect(element.textContent).not.toContain(isCurated ? 'MEAL_PLANS.MY_PLAN' : 'MEAL_PLANS.CURATED');
        expect(planOpen).toHaveBeenCalledWith('plan-1');
    });

    it('renders loader while loading', () => {
        const fixture = createComponent({ isLoading: true, plans: [createPlanCard()] });
        const element = getElement(fixture);

        expect(element.querySelector('.meal-plans-list__loading')).not.toBeNull();
        expect(element.querySelector('fd-ui-loader')).not.toBeNull();
        expect(element.querySelector('.meal-plan-card')).toBeNull();
    });

    it('updates calorie grouping when the interface language changes', () => {
        const fixture = createComponent();
        const element = getElement(fixture);
        const translateService = TestBed.inject(TranslateService);

        translateService.use('ru');
        fixture.detectChanges();
        expect(element.textContent).toContain('~1\u00A0800');
        expect(element.textContent).not.toContain('1,800');
        translateService.use('en');
        fixture.detectChanges();
        expect(element.textContent).toContain('~1,800');
    });

    /* eslint-disable @typescript-eslint/no-magic-numbers -- Counts cover English and Russian plural boundaries. */
    it.each([
        ['en', 1, 'ONE'],
        ['en', 2, 'OTHER'],
        ['ru', 1, 'ONE'],
        ['ru', 2, 'FEW'],
        ['ru', 5, 'MANY'],
        ['ru', 11, 'MANY'],
        ['ru', 21, 'ONE'],
    ])('updates card plural labels for %s count %i', (language, count, category) => {
        const fixture = createComponent({
            plans: [{ ...createPlanCard(), durationDays: planDurationDaysFromStored(Number(count)), totalRecipes: Number(count) }],
        });
        TestBed.inject(TranslateService).use(String(language));
        fixture.detectChanges();
        const text = getElement(fixture).querySelector('.meal-plan-card__meta')?.textContent;
        expect(text).toContain(`MEAL_PLANS.DAYS_${category}`);
        expect(text).toContain(`MEAL_PLANS.RECIPES_${category}`);
    });
    /* eslint-enable @typescript-eslint/no-magic-numbers -- Restore the rule after plural boundary cases. */
});

function createComponent(
    overrides: Partial<{ isLoading: boolean; plans: MealPlanCardViewModel[] }> = {},
): ComponentFixture<MealPlanListContentComponent> {
    const fixture = TestBed.createComponent(MealPlanListContentComponent);
    fixture.componentRef.setInput('isLoading', overrides.isLoading ?? false);
    fixture.componentRef.setInput('plans', overrides.plans ?? [createPlanCard()]);
    fixture.detectChanges();

    return fixture;
}

function getElement(fixture: ComponentFixture<MealPlanListContentComponent>): HTMLElement {
    return fixture.nativeElement as HTMLElement;
}

function createPlanCard(): MealPlanCardViewModel {
    return {
        id: entityId<'meal-plan'>('plan-1'),
        name: 'Keto plan',
        description: null,
        dietType: 'Keto',
        durationDays: planDurationDaysFromStored(TEST_PLAN_DURATION_DAYS),
        targetCaloriesPerDay: 1800,
        isCurated: true,
        totalRecipes: 21,
        dietTypeKey: 'MEAL_PLANS.DIET_TYPE.KETO',
    };
}
