import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { TranslateModule } from '@ngx-translate/core';
import { describe, expect, it } from 'vitest';

import { DEFAULT_SATIETY_LEVEL } from '../../../shared/lib/satiety-level.utils';
import { MealDetailsFieldsComponent } from './meal-details-fields.component';

const INVALID_SATIETY_LEVEL = 99;
const MAX_SATIETY_LEVEL = 5;

async function setupMealDetailsFieldsAsync(): Promise<ComponentFixture<MealDetailsFieldsComponent>> {
    await TestBed.configureTestingModule({
        imports: [MealDetailsFieldsComponent, TranslateModule.forRoot()],
    }).compileComponents();

    const fixture = TestBed.createComponent(MealDetailsFieldsComponent);
    fixture.componentRef.setInput('date', '2026-05-17');
    fixture.componentRef.setInput('time', '12:30');
    fixture.componentRef.setInput('comment', '');
    return fixture;
}

describe('MealDetailsFieldsComponent satiety', () => {
    it('normalizes invalid satiety values to default', async () => {
        const fixture = await setupMealDetailsFieldsAsync();
        const component = fixture.componentInstance;
        fixture.detectChanges();

        component.onPreMealSatietyLevelChange(null);
        component.onPostMealSatietyLevelChange(INVALID_SATIETY_LEVEL);

        expect(component.preMealSatietyLevel()).toBe(DEFAULT_SATIETY_LEVEL);
        expect(component.postMealSatietyLevel()).toBe(MAX_SATIETY_LEVEL);
    });
});
