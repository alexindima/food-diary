import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { describe, expect, it } from 'vitest';

import { provideTranslateTesting } from '../../../../../../testing/translate-testing.module';
import type { IngredientPreviewItem } from '../recipe-detail-lib/recipe-detail.types';
import { RecipeDetailIngredientPreviewComponent } from './recipe-detail-ingredient-preview';

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

    it('expands all ingredients and collapses back without losing the last item', async () => {
        const count = 7;
        const previewCount = 5;
        const fixture = await setupComponentAsync(
            Array.from({ length: count }, (_, index) => ({
                name: `Ingredient ${index}`,
                amount: 1,
                unitKey: null,
            })),
        );
        const element = fixture.nativeElement as HTMLElement;
        const button = element.querySelector('button');
        if (button === null) {
            throw new Error('Missing ingredient expansion control');
        }
        expect(element.querySelectorAll('.recipe-detail__list-row')).toHaveLength(previewCount);
        expect(button.getAttribute('aria-expanded')).toBe('false');
        button.click();
        fixture.detectChanges();
        expect(element.querySelectorAll('.recipe-detail__list-row')).toHaveLength(count);
        expect(element.textContent).toContain('Ingredient 6');
        expect(button.getAttribute('aria-expanded')).toBe('true');
        button.click();
        fixture.detectChanges();
        expect(element.querySelectorAll('.recipe-detail__list-row')).toHaveLength(previewCount);
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
        imports: [RecipeDetailIngredientPreviewComponent],
        providers: [provideTranslateTesting()],
    }).compileComponents();

    const fixture = TestBed.createComponent(RecipeDetailIngredientPreviewComponent);
    fixture.componentRef.setInput('ingredients', ingredients);
    fixture.detectChanges();

    return fixture;
}

function getTextContent(fixture: ComponentFixture<RecipeDetailIngredientPreviewComponent>): string {
    return (fixture.nativeElement as HTMLElement).textContent;
}
