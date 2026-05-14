import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { TranslateModule } from '@ngx-translate/core';
import { describe, expect, it } from 'vitest';

import type { IngredientPreviewItem } from '../recipe-detail-lib/recipe-detail.types';
import { RecipeDetailIngredientPreviewComponent } from './recipe-detail-ingredient-preview.component';

describe('RecipeDetailIngredientPreviewComponent', () => {
    it('renders ingredient preview rows with amounts and units', async () => {
        const fixture = await setupComponentAsync([
            { name: 'Flour', amount: 200, unitKey: 'GENERAL.UNITS.G' },
            { name: 'Starter', amount: 1, unitKey: null },
        ]);

        const text = getTextContent(fixture);

        expect(text).toContain('Flour');
        expect(text).toContain('200');
        expect(text).toContain('GENERAL.UNITS.G');
        expect(text).toContain('Starter');
    });

    it('renders nothing when ingredient list is empty', async () => {
        const fixture = await setupComponentAsync([]);

        expect(getTextContent(fixture).trim()).toBe('');
    });
});

async function setupComponentAsync(
    ingredients: readonly IngredientPreviewItem[],
): Promise<ComponentFixture<RecipeDetailIngredientPreviewComponent>> {
    await TestBed.configureTestingModule({
        imports: [RecipeDetailIngredientPreviewComponent, TranslateModule.forRoot()],
    }).compileComponents();

    const fixture = TestBed.createComponent(RecipeDetailIngredientPreviewComponent);
    fixture.componentRef.setInput('ingredients', ingredients);
    fixture.detectChanges();

    return fixture;
}

function getTextContent(fixture: ComponentFixture<RecipeDetailIngredientPreviewComponent>): string {
    return (fixture.nativeElement as HTMLElement).textContent;
}
