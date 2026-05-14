import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { TranslateModule } from '@ngx-translate/core';
import { describe, expect, it } from 'vitest';

import { type Recipe, RecipeVisibility } from '../../../models/recipe.data';
import { RecipeDetailSummaryComponent } from './recipe-detail-summary.component';

const RECIPE_CALORIES = 240;
const TOTAL_TIME_MINUTES = 45;
const QUALITY_SCORE_GREEN = 90;
const PROTEIN_VALUE = 20;
const PROTEIN_PERCENT = 50;
const INGREDIENT_AMOUNT = 100;

describe('RecipeDetailSummaryComponent', () => {
    it('derives quality hint from quality grade', () => {
        const { component } = setupComponent();

        expect(component.qualityHintKey()).toBe('QUALITY.GREEN');
    });

    it('renders summary values and ingredient preview', () => {
        const { fixture } = setupComponent();
        const text = getText(fixture);

        expect(text).toContain(RECIPE_CALORIES.toString());
        expect(text).toContain('2');
        expect(text).toContain('Rice');
    });
});

function setupComponent(): { fixture: ComponentFixture<RecipeDetailSummaryComponent>; component: RecipeDetailSummaryComponent } {
    TestBed.configureTestingModule({
        imports: [RecipeDetailSummaryComponent, TranslateModule.forRoot()],
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
