import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { describe, expect, it } from 'vitest';

import type { Meal } from '../../../models/meal.data';
import { MealEditComponent } from './meal-edit.component';

describe('MealEditComponent', () => {
    it('should default meal input to null', () => {
        const { component } = setupComponent();

        expect(component.consumption()).toBeNull();
    });

    it('should accept meal input for edit form wrapper', () => {
        const meal = createMeal();
        const { component, fixture } = setupComponent();

        fixture.componentRef.setInput('consumption', meal);
        fixture.detectChanges();

        expect(component.consumption()).toEqual(meal);
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
        id: 'meal-1',
        date: '2026-05-14T12:00:00Z',
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
