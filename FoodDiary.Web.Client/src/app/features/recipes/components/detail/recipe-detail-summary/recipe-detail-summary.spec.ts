import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { describe, expect, it } from 'vitest';

import { provideTranslateTesting } from '../../../../../../testing/translate-testing.module';
import { type Recipe, RecipeVisibility } from '../../../models/recipe.data';
import { RecipeDetailSummaryComponent, resolveServingsUnitKey } from './recipe-detail-summary';

const RECIPE_CALORIES = 240;
const TOTAL_TIME_MINUTES = 45;
const QUALITY_SCORE_GREEN = 90;
const PROTEIN_VALUE = 20;
const PROTEIN_PERCENT = 50;
const INGREDIENT_AMOUNT = 100;

describe('RecipeDetailSummaryComponent', () => {
    it('derives quality hint from quality grade', () => {
        const { component } = setupComponent();

        expect(component['qualityHintKey']()).toBe('QUALITY.GREEN');
    });

    it('renders summary values and ingredient preview', () => {
        const { fixture } = setupComponent();
        const text = getText(fixture);

        expect(text).toContain(RECIPE_CALORIES.toString());
        expect(text).toContain('2');
        expect(text).toContain('Rice');
    });
});

describe('resolveServingsUnitKey', () => {
    /* eslint-disable @typescript-eslint/no-magic-numbers -- The table covers Russian pluralization boundaries. */
    it.each([
        [1, 'SERVINGS_ONE'],
        [2, 'SERVINGS_FEW'],
        [4, 'SERVINGS_FEW'],
        [5, 'SERVINGS_MANY'],
        [11, 'SERVINGS_MANY'],
        [21, 'SERVINGS_ONE'],
        [22, 'SERVINGS_FEW'],
        [25, 'SERVINGS_MANY'],
    ])('selects the correct plural form for %i servings', (count, suffix) => {
        expect(resolveServingsUnitKey(count)).toBe(`RECIPE_DETAIL.SUMMARY.${suffix}`);
    });
    /* eslint-enable @typescript-eslint/no-magic-numbers -- Re-enable the rule after the boundary table. */
});

function setupComponent(): { fixture: ComponentFixture<RecipeDetailSummaryComponent>; component: RecipeDetailSummaryComponent } {
    TestBed.configureTestingModule({
        imports: [RecipeDetailSummaryComponent],
        providers: [provideTranslateTesting()],
    });

    const fixture = TestBed.createComponent(RecipeDetailSummaryComponent);
    fixture.componentRef.setInput('recipe', createRecipe());
    fixture.componentRef.setInput('calories', RECIPE_CALORIES);
    fixture.componentRef.setInput('totalTime', TOTAL_TIME_MINUTES);
    fixture.componentRef.setInput('qualityGrade', 'green');
    fixture.componentRef.setInput('qualityScore', QUALITY_SCORE_GREEN);
    fixture.componentRef.setInput('macroSummaryBlocks', [
        {
            labelKey: 'GENERAL.NUTRIENTS.PROTEIN',
            value: PROTEIN_VALUE,
            unitKey: 'GENERAL.UNITS.G',
            color: '#000',
            percent: PROTEIN_PERCENT,
        },
    ]);
    fixture.componentRef.setInput('ingredientCount', 1);
    fixture.componentRef.setInput('ingredientPreview', [{ name: 'Rice', amount: INGREDIENT_AMOUNT, unitKey: 'GENERAL.UNITS.G' }]);
    fixture.detectChanges();

    return { fixture, component: fixture.componentInstance };
}

function getText(fixture: ComponentFixture<RecipeDetailSummaryComponent>): string {
    return (fixture.nativeElement as HTMLElement).textContent;
}

function createRecipe(): Recipe {
    return {
        id: 'recipe-1',
        name: 'Recipe',
        servings: 2,
        visibility: RecipeVisibility.Private,
        usageCount: 0,
        createdAt: '2026-01-01T00:00:00Z',
        isOwnedByCurrentUser: true,
        isNutritionAutoCalculated: true,
        steps: [],
    };
}
