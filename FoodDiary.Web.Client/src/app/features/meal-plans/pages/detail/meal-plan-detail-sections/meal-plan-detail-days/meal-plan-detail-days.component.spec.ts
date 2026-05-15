import type { ComponentFixture } from '@angular/core/testing';
import { TestBed } from '@angular/core/testing';
import { TranslateModule } from '@ngx-translate/core';
import { beforeEach, describe, expect, it } from 'vitest';

import type { MealPlanDayViewModel } from '../../../../lib/meal-plan-view.mapper';
import { MealPlanDetailDaysComponent } from './meal-plan-detail-days.component';

describe('MealPlanDetailDaysComponent', () => {
    beforeEach(() => {
        TestBed.configureTestingModule({
            imports: [MealPlanDetailDaysComponent, TranslateModule.forRoot()],
        });
    });

    it('renders day meals and nutrition items', () => {
        const fixture = createComponent([createDay()]);
        const textContent = getElement(fixture).textContent;

        expect(textContent).toContain('Omelette');
        expect(textContent).toContain('450');
        expect(textContent).toContain('P:');
    });
});

function getElement(fixture: ComponentFixture<MealPlanDetailDaysComponent>): HTMLElement {
    return fixture.nativeElement as HTMLElement;
}

function createComponent(days: MealPlanDayViewModel[]): ComponentFixture<MealPlanDetailDaysComponent> {
    const fixture = TestBed.createComponent(MealPlanDetailDaysComponent);
    fixture.componentRef.setInput('days', days);
    fixture.detectChanges();

    return fixture;
}

function createDay(): MealPlanDayViewModel {
    return {
        id: 'day-1',
        dayNumber: 1,
        meals: [
            {
                id: 'meal-1',
                mealType: 'Breakfast',
                recipeId: 'recipe-1',
                recipeName: 'Omelette',
                servings: 1,
                calories: 450,
                mealTypeKey: 'MEAL_PLANS.MEAL_TYPE.BREAKFAST',
                nutritionItems: [
                    { unitKey: 'GENERAL.UNITS.KCAL', value: 450, prefix: '' },
                    { unitKey: 'GENERAL.UNITS.G', value: 30, prefix: 'P: ' },
                ],
            },
        ],
    };
}
