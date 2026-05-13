import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { TranslateModule } from '@ngx-translate/core';
import { describe, expect, it } from 'vitest';

import { MealSatietyFieldsComponent } from './meal-satiety-fields.component';

const LEGACY_SATIETY_LEVEL = 8;
const NORMALIZED_LEGACY_SATIETY_LEVEL = 4;
const BELOW_MIN_POSITIVE_SATIETY_LEVEL = 0.5;
const MIN_SATIETY_LEVEL = 1;

describe('MealSatietyFieldsComponent', () => {
    it('should normalize and emit pre meal satiety changes', async () => {
        const { component } = await setupComponentAsync();

        component.onSatietyLevelChange('before', LEGACY_SATIETY_LEVEL);

        expect(component.preMealSatietyLevel()).toBe(NORMALIZED_LEGACY_SATIETY_LEVEL);
    });

    it('should normalize and emit post meal satiety changes', async () => {
        const { component } = await setupComponentAsync();

        component.onSatietyLevelChange('after', BELOW_MIN_POSITIVE_SATIETY_LEVEL);

        expect(component.postMealSatietyLevel()).toBe(MIN_SATIETY_LEVEL);
    });

    it('should build translated aria labels from configured label keys', async () => {
        const { component, fixture } = await setupComponentAsync();
        fixture.componentRef.setInput('labelBeforeKey', 'CONSUMPTION_MANAGE.HUNGER_BEFORE_LABEL');
        fixture.detectChanges();

        expect(component.preMealSatietyAriaLabel()).toContain('CONSUMPTION_MANAGE.HUNGER_BEFORE_LABEL');
    });
});

type MealSatietyFieldsSetup = {
    component: MealSatietyFieldsComponent;
    fixture: ComponentFixture<MealSatietyFieldsComponent>;
};

async function setupComponentAsync(): Promise<MealSatietyFieldsSetup> {
    await TestBed.configureTestingModule({
        imports: [MealSatietyFieldsComponent, TranslateModule.forRoot()],
    }).compileComponents();

    const fixture = TestBed.createComponent(MealSatietyFieldsComponent);
    fixture.detectChanges();

    return {
        component: fixture.componentInstance,
        fixture,
    };
}
