import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { describe, expect, it } from 'vitest';

import type { Meal } from '../../../../../shared/models/meal.data';
import { utcInstant } from '../../../../../shared/models/semantics/date-value';
import { entityId } from '../../../../../shared/models/semantics/entity-id';
import { MealEditComponent } from './meal-edit';

describe('MealEditComponent', () => {
    it('should default meal input to null', () => {
        const { component } = setupComponent();

        expect(component['meal']()).toBeNull();
    });

    it('should accept meal input for edit form wrapper', () => {
        const meal = createMeal();
        const { component, fixture } = setupComponent();

        fixture.componentRef.setInput('meal', meal);
        fixture.detectChanges();

        expect(component['meal']()).toEqual(meal);
    });
});

function setupComponent(): {
    component: MealEditComponent;
    fixture: ComponentFixture<MealEditComponent>;
} {
    TestBed.configureTestingModule({
        imports: [MealEditComponent],
    });
    TestBed.overrideComponent(MealEditComponent, {
        set: { template: '' },
    });

    const fixture = TestBed.createComponent(MealEditComponent);
    fixture.detectChanges();

    return {
        component: fixture.componentInstance,
        fixture,
    };
}

function createMeal(overrides: Partial<Meal> = {}): Meal {
    return {
        id: entityId<'meal'>('meal-1'),
        date: utcInstant('2026-05-14T12:00:00Z'),
        mealType: 'LUNCH',
        comment: null,
        imageUrl: null,
        imageAssetId: null,
        totalCalories: 500,
        totalProteins: 30,
        totalFats: 20,
        totalCarbs: 50,
        totalFiber: 5,
        totalAlcohol: 0,
        isNutritionAutoCalculated: true,
        preMealSatietyLevel: null,
        postMealSatietyLevel: null,
        items: [],
        aiSessions: [],
        ...overrides,
    };
}
