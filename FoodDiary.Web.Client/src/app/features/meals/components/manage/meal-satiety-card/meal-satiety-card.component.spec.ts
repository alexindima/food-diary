import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { TranslateModule } from '@ngx-translate/core';
import { describe, expect, it, vi } from 'vitest';

import { MealSatietyCardComponent } from './meal-satiety-card.component';

const PRE_MEAL_SATIETY_LEVEL = 2;
const NEXT_PRE_MEAL_SATIETY_LEVEL = 4;
const POST_MEAL_SATIETY_LEVEL = 3;

describe('MealSatietyCardComponent', () => {
    it('should emit pre meal satiety changes', async () => {
        const { component } = await setupComponentAsync();
        const handler = vi.fn();
        component.preMealSatietyLevelChange.subscribe(handler);

        component.preMealSatietyLevelChange.emit(NEXT_PRE_MEAL_SATIETY_LEVEL);

        expect(handler).toHaveBeenCalledWith(NEXT_PRE_MEAL_SATIETY_LEVEL);
    });
});

type MealSatietyCardSetup = {
    component: MealSatietyCardComponent;
    fixture: ComponentFixture<MealSatietyCardComponent>;
};

async function setupComponentAsync(): Promise<MealSatietyCardSetup> {
    await TestBed.configureTestingModule({
        imports: [MealSatietyCardComponent, TranslateModule.forRoot()],
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
