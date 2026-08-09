import { TestBed } from '@angular/core/testing';
import { describe, expect, it } from 'vitest';

import { provideTranslateTesting } from '../../../../../testing/translate-testing.module';
import { StatisticsMealStructureCardComponent } from './statistics-meal-structure-card';

const MEAL_TYPE_COUNT = 4;

describe('StatisticsMealStructureCardComponent', () => {
    it('renders the meal distribution and summary', async () => {
        await TestBed.configureTestingModule({
            imports: [StatisticsMealStructureCardComponent],
            providers: [provideTranslateTesting()],
        }).compileComponents();
        const fixture = TestBed.createComponent(StatisticsMealStructureCardComponent);
        fixture.componentRef.setInput('data', {
            totalCalories: 1200,
            averageMealsPerDay: 3.2,
            dominantMeal: 'lunch',
            items: [
                { key: 'breakfast', calories: 300, percentage: 25 },
                { key: 'lunch', calories: 480, percentage: 40 },
                { key: 'dinner', calories: 360, percentage: 30 },
                { key: 'snack', calories: 60, percentage: 5 },
            ],
        });
        fixture.detectChanges();
        const root = fixture.nativeElement as HTMLElement;
        expect(root.querySelectorAll('.statistics-meal-structure-card__bar-segment')).toHaveLength(MEAL_TYPE_COUNT);
        expect(root.textContent).toContain('40%');
        expect(root.textContent).toContain('3.2');
    });
});
