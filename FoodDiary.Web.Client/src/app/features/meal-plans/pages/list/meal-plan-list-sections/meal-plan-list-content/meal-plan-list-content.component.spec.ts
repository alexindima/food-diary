import type { ComponentFixture } from '@angular/core/testing';
import { TestBed } from '@angular/core/testing';
import { TranslateModule } from '@ngx-translate/core';
import { beforeEach, describe, expect, it, vi } from 'vitest';

import type { MealPlanCardViewModel } from '../../../../lib/meal-plan-view.mapper';
import { MealPlanListContentComponent } from './meal-plan-list-content.component';

describe('MealPlanListContentComponent', () => {
    beforeEach(() => {
        TestBed.configureTestingModule({
            imports: [MealPlanListContentComponent, TranslateModule.forRoot()],
        });
    });

    it('renders empty state when there are no plans', () => {
        const fixture = createComponent({ plans: [] });
        const element = getElement(fixture);

        expect(element.querySelector('.meal-plans-list__empty')).not.toBeNull();
        expect(element.querySelector('.meal-plan-card')).toBeNull();
    });

    it('renders plan cards and emits opened plan id', () => {
        const fixture = createComponent({ plans: [createPlanCard()] });
        const element = getElement(fixture);
        const planOpen = vi.fn();
        fixture.componentInstance.planOpen.subscribe(planOpen);

        element.querySelector<HTMLElement>('.meal-plan-card')?.click();

        expect(element.querySelector('.meal-plan-card__name')?.textContent).toContain('Keto plan');
        expect(planOpen).toHaveBeenCalledWith('plan-1');
    });

    it('renders loader while loading', () => {
        const fixture = createComponent({ isLoading: true, plans: [createPlanCard()] });
        const element = getElement(fixture);

        expect(element.querySelector('fd-ui-loader')).not.toBeNull();
        expect(element.querySelector('.meal-plan-card')).toBeNull();
    });
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
        id: 'plan-1',
        name: 'Keto plan',
        description: null,
        dietType: 'Keto',
        durationDays: 7,
        targetCaloriesPerDay: 1800,
        isCurated: true,
        totalRecipes: 21,
        dietTypeKey: 'MEAL_PLANS.DIET_TYPE.KETO',
    };
}
