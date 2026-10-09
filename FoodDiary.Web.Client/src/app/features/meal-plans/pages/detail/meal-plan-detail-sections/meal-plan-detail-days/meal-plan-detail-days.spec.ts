import type { ComponentFixture } from '@angular/core/testing';
import { TestBed } from '@angular/core/testing';
import { TranslateService } from '@ngx-translate/core';
import { beforeEach, describe, expect, it } from 'vitest';

import { provideTranslateTesting } from '../../../../../../../testing/translate-testing.module';
import { entityId } from '../../../../../../shared/models/semantics/entity-id';
import type { MealPlanDayViewModel } from '../../../../lib/meal-plan-view.mapper';
import { plannedServingsFromStored } from '../../../../models/meal-plan-values';
import { plannedMealTypeFromStored } from '../../../../models/meal-plan-values';
import { planDayNumberFromStored } from '../../../../models/meal-plan-values';
import { MealPlanDetailDaysComponent } from './meal-plan-detail-days';

describe('MealPlanDetailDaysComponent', () => {
    beforeEach(() => {
        TestBed.configureTestingModule({
            imports: [MealPlanDetailDaysComponent],
            providers: [provideTranslateTesting()],
        });
    });

    it('renders day meals and nutrition items', () => {
        const fixture = createComponent([createDay()]);
        const textContent = getElement(fixture).textContent;

        expect(textContent).toContain('Omelette');
        expect(textContent).toContain('450');
        expect(textContent).toContain('GENERAL.NUTRIENTS.PROTEIN');
    });

    it('updates large nutrient values when the language changes', () => {
        const day = createDay();
        day.meals[0].nutritionItems[0].value = 1500;
        const fixture = createComponent([day]);
        const element = getElement(fixture);
        const translateService = TestBed.inject(TranslateService);

        translateService.use('ru');
        fixture.detectChanges();
        expect(element.textContent).toContain('1\u00A0500');
        translateService.use('en');
        fixture.detectChanges();
        expect(element.textContent).toContain('1,500');
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
        id: entityId<'meal-plan-day'>('day-1'),
        dayNumber: planDayNumberFromStored(1),
        meals: [
            {
                id: entityId<'meal-plan-meal'>('meal-1'),
                mealType: plannedMealTypeFromStored('Breakfast'),
                recipeId: entityId<'recipe'>('recipe-1'),
                recipeName: 'Omelette',
                servings: plannedServingsFromStored(1),
                calories: 450,
                mealTypeKey: 'MEAL_PLANS.MEAL_TYPE.BREAKFAST',
                nutritionItems: [
                    { unitKey: 'GENERAL.UNITS.KCAL', value: 450, prefix: '' },
                    { unitKey: 'GENERAL.UNITS.G', value: 30, prefix: 'GENERAL.NUTRIENTS.PROTEIN' },
                ],
            },
        ],
    };
}
