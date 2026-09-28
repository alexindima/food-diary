import { TestBed } from '@angular/core/testing';
import { By } from '@angular/platform-browser';
import { TranslateService } from '@ngx-translate/core';
import { FdUiHintDirective } from 'fd-ui-kit';
import { firstValueFrom } from 'rxjs';
import { beforeEach, describe, expect, it } from 'vitest';

import { provideTranslateTesting } from '../../../../../testing/translate-testing.module';
import { publicRecipeFixture } from '../../lib/public-recipe.test-helper';
import { PublicNutritionComponent } from './public-nutrition';

describe('public nutrition completeness hints', () => {
    beforeEach(() => TestBed.configureTestingModule({ imports: [PublicNutritionComponent], providers: [provideTranslateTesting()] }));

    it.each([
        { names: ['Salt', 'Herbs'], count: 2, expected: '2 of 2: Salt, Herbs' },
        { names: ['Salt'], count: 4, expected: '4, including Salt' },
        { names: [], count: 4, expected: '4 omitted' },
    ])('uses a denominator only for a complete direct ingredient list ($expected)', async ({ names, count, expected }) => {
        const translate = TestBed.inject(TranslateService);
        translate.setTranslation('en', {
            PUBLIC_RECIPES: {
                PARTIAL_HINT_NAMES: '{{count}} of {{total}}: {{names}}',
                PARTIAL_HINT_SOME_NAMES: '{{count}}, including {{names}}',
                PARTIAL_HINT: '{{count}} omitted',
            },
        });
        await firstValueFrom(translate.use('en'));
        const recipe = publicRecipeFixture();
        recipe.missingIngredientNames = names;
        recipe.missingIngredientCount = count;
        recipe.steps[0].ingredients = ['Salt', 'Herbs'].map(name => ({
            name,
            amount: null,
            unit: null,
            amountText: 'to taste',
            recipeId: null,
            isAvailable: true,
        }));
        const fixture = TestBed.createComponent(PublicNutritionComponent);
        fixture.componentRef.setInput('recipe', recipe);
        fixture.detectChanges();
        const hint = fixture.debugElement.query(By.directive(FdUiHintDirective)).injector.get(FdUiHintDirective);
        expect(hint.fdUiHint()).toBe(expected);
    });
});

describe('public nutrition values', () => {
    beforeEach(() => TestBed.configureTestingModule({ imports: [PublicNutritionComponent], providers: [provideTranslateTesting()] }));

    it('updates decimal separators when the interface language changes', async () => {
        const translate = TestBed.inject(TranslateService);
        translate.setTranslation('en', {});
        translate.setTranslation('ru', {});
        await firstValueFrom(translate.use('en'));
        const fixture = TestBed.createComponent(PublicNutritionComponent);
        fixture.componentRef.setInput('recipe', { ...publicRecipeFixture(), totalProteins: 73 });
        fixture.detectChanges();
        const element: unknown = fixture.nativeElement;
        if (!(element instanceof HTMLElement)) {
            throw new Error('Expected component element');
        }
        expect(element.textContent).toContain('36.5');
        await firstValueFrom(translate.use('ru'));
        fixture.detectChanges();
        expect(element.textContent).toContain('36,5');
        expect(element.textContent).not.toContain('36.5');
    });

    it.each([
        { value: null, missing: 8, grid: false, warning: false, detail: true },
        { value: null, missing: 0, grid: false, warning: false, detail: false },
        { value: 200, missing: 2, grid: true, warning: true, detail: false },
        { value: 200, missing: 0, grid: true, warning: false, detail: false },
        { value: 0, missing: 0, grid: true, warning: false, detail: false },
    ])('renders values=$value, missing=$missing without confusing zero with missing data', ({ value, missing, grid, warning, detail }) => {
        const recipe = {
            ...publicRecipeFixture(),
            totalCalories: value,
            totalProteins: value,
            totalFats: value,
            totalCarbs: value,
            totalFiber: value,
            totalAlcohol: value,
            missingIngredientCount: missing,
        };
        const fixture = TestBed.createComponent(PublicNutritionComponent);
        fixture.componentRef.setInput('recipe', recipe);
        fixture.detectChanges();
        const element: unknown = fixture.nativeElement;
        if (!(element instanceof HTMLElement)) {
            throw new Error('Expected component element');
        }
        expect(element.querySelector('.nutrition-grid') !== null).toBe(grid);
        expect(element.querySelector('.nutrition-warning') !== null).toBe(warning);
        expect(element.querySelector('.nutrition-empty') !== null).toBe(!grid);
        expect(element.querySelector('.nutrition-empty-detail') !== null).toBe(detail);
    });
});
