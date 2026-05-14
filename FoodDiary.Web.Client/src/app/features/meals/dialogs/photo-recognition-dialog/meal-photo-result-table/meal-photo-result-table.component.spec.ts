import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { TranslateModule } from '@ngx-translate/core';
import { describe, expect, it } from 'vitest';

import type { RecognizedItemView } from '../meal-photo-recognition-dialog-lib/meal-photo-recognition-dialog.types';
import { MealPhotoResultTableComponent } from './meal-photo-result-table.component';

describe('MealPhotoResultTableComponent', () => {
    it('should render recognized items with translated and raw units', async () => {
        const { fixture } = await setupComponentAsync({
            resultViews: [
                createItemView({ displayName: 'Apple', amount: 120, unitKey: 'PRODUCT_AMOUNT_UNITS.G', unit: 'g' }),
                createItemView({ displayName: 'Tea spoon', amount: 1, unitKey: null, unit: 'spoon' }),
            ],
        });

        fixture.detectChanges();

        const text = getFixtureText(fixture);
        expect(text).toContain('CONSUMPTION_MANAGE.PHOTO_AI_DIALOG.NAME_LABEL');
        expect(text).toContain('Apple');
        expect(text).toContain('PRODUCT_AMOUNT_UNITS.G');
        expect(text).toContain('Tea spoon');
        expect(text).toContain('spoon');
    });
});

async function setupComponentAsync(
    overrides: Partial<{
        resultViews: RecognizedItemView[];
    }> = {},
): Promise<{
    component: MealPhotoResultTableComponent;
    fixture: ComponentFixture<MealPhotoResultTableComponent>;
}> {
    await TestBed.resetTestingModule()
        .configureTestingModule({
            imports: [MealPhotoResultTableComponent, TranslateModule.forRoot()],
        })
        .compileComponents();

    const fixture = TestBed.createComponent(MealPhotoResultTableComponent);
    fixture.componentRef.setInput('resultViews', overrides.resultViews ?? [createItemView()]);

    return {
        component: fixture.componentInstance,
        fixture,
    };
}

function getFixtureText(fixture: ComponentFixture<MealPhotoResultTableComponent>): string {
    const host = fixture.nativeElement as HTMLElement;
    return host.textContent;
}

function createItemView(overrides: Partial<RecognizedItemView> = {}): RecognizedItemView {
    return {
        item: { nameEn: 'Apple', nameLocal: null, amount: 100, unit: 'g', confidence: 0.9 },
        displayName: 'Apple',
        amount: 100,
        unit: 'g',
        unitKey: 'PRODUCT_AMOUNT_UNITS.G',
        ...overrides,
    };
}
