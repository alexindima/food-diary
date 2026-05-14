import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { TranslateModule } from '@ngx-translate/core';
import { describe, expect, it } from 'vitest';

import type { MacroSummaryItem } from '../meal-photo-recognition-dialog-lib/meal-photo-recognition-dialog.types';
import { MealPhotoNutritionSummaryComponent } from './meal-photo-nutrition-summary.component';

describe('MealPhotoNutritionSummaryComponent', () => {
    it('should render macro summary when items exist', async () => {
        const { fixture } = await setupComponentAsync({ items: createMacroItems() });

        fixture.detectChanges();

        const text = getFixtureText(fixture);
        expect(text).toContain('CONSUMPTION_MANAGE.PHOTO_AI_DIALOG.NUTRITION_TITLE');
        expect(text).toContain('NUTRIENTS.CALORIES');
        expect(text).toContain('GENERAL.UNITS.KCAL');
    });

    it('should render nutrition error when there are no items', async () => {
        const { fixture } = await setupComponentAsync({ items: [], errorKey: 'ERRORS.NUTRITION_FAILED' });

        fixture.detectChanges();

        expect(getFixtureText(fixture)).toContain('ERRORS.NUTRITION_FAILED');
    });

    it('should hide summary and error while nutrition is loading', async () => {
        const { fixture } = await setupComponentAsync({
            isNutritionLoading: true,
            items: createMacroItems(),
            errorKey: 'ERRORS.NUTRITION_FAILED',
        });

        fixture.detectChanges();

        expect(getFixtureText(fixture).trim()).toBe('');
    });
});

async function setupComponentAsync(
    overrides: Partial<{
        errorKey: string | null;
        isNutritionLoading: boolean;
        items: MacroSummaryItem[];
    }> = {},
): Promise<{
    component: MealPhotoNutritionSummaryComponent;
    fixture: ComponentFixture<MealPhotoNutritionSummaryComponent>;
}> {
    await TestBed.resetTestingModule()
        .configureTestingModule({
            imports: [MealPhotoNutritionSummaryComponent, TranslateModule.forRoot()],
        })
        .compileComponents();

    const fixture = TestBed.createComponent(MealPhotoNutritionSummaryComponent);
    fixture.componentRef.setInput('isNutritionLoading', overrides.isNutritionLoading ?? false);
    fixture.componentRef.setInput('items', overrides.items ?? []);
    fixture.componentRef.setInput('errorKey', overrides.errorKey ?? null);

    return {
        component: fixture.componentInstance,
        fixture,
    };
}

function getFixtureText(fixture: ComponentFixture<MealPhotoNutritionSummaryComponent>): string {
    const host = fixture.nativeElement as HTMLElement;
    return host.textContent;
}

function createMacroItems(): MacroSummaryItem[] {
    return [
        {
            key: 'calories',
            labelKey: 'NUTRIENTS.CALORIES',
            value: 120,
            unitKey: 'GENERAL.UNITS.KCAL',
            numberFormat: '1.0-0',
        },
    ];
}
