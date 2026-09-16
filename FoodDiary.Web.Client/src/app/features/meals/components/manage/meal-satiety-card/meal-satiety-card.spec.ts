import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { By } from '@angular/platform-browser';
import { describe, expect, it, vi } from 'vitest';

import { provideTranslateTesting } from '../../../../../../testing/translate-testing.module';
import { MealSatietyFieldsComponent } from '../../../../../components/shared/meal-satiety-fields/meal-satiety-fields';
import { MealSatietyCardComponent } from './meal-satiety-card';

const PRE_MEAL_SATIETY_LEVEL = 2;
const NEXT_PRE_MEAL_SATIETY_LEVEL = 4;
const POST_MEAL_SATIETY_LEVEL = 3;

describe('MealSatietyCardComponent', () => {
    it('should emit pre meal satiety changes', async () => {
        const { component, fixture } = await setupComponentAsync();
        const handler = vi.fn();
        component.preMealSatietyLevel.subscribe(handler);
        const fields = fixture.debugElement.query(By.directive(MealSatietyFieldsComponent)).injector.get(MealSatietyFieldsComponent);

        fields.preMealSatietyLevel.set(NEXT_PRE_MEAL_SATIETY_LEVEL);
        fixture.detectChanges();

        expect(component.preMealSatietyLevel()).toBe(NEXT_PRE_MEAL_SATIETY_LEVEL);
        expect(handler).toHaveBeenCalledTimes(1);
        expect(handler).toHaveBeenCalledWith(NEXT_PRE_MEAL_SATIETY_LEVEL);
    });
});

type MealSatietyCardSetup = {
    component: MealSatietyCardComponent;
    fixture: ComponentFixture<MealSatietyCardComponent>;
};

async function setupComponentAsync(): Promise<MealSatietyCardSetup> {
    await TestBed.configureTestingModule({
        imports: [MealSatietyCardComponent],
        providers: [provideTranslateTesting()],
    }).compileComponents();

    const fixture = TestBed.createComponent(MealSatietyCardComponent);
    fixture.componentRef.setInput('preMealSatietyLevel', PRE_MEAL_SATIETY_LEVEL);
    fixture.componentRef.setInput('postMealSatietyLevel', POST_MEAL_SATIETY_LEVEL);
    fixture.detectChanges();

    return {
        component: fixture.componentInstance,
        fixture,
    };
}
