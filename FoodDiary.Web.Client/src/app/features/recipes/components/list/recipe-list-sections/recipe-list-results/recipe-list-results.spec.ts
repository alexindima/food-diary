import { DOCUMENT } from '@angular/common';
import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { TranslateService } from '@ngx-translate/core';
import { firstValueFrom } from 'rxjs';
import { afterEach, describe, expect, it, vi } from 'vitest';

import { provideTranslateTesting } from '../../../../../../../testing/translate-testing.module';
import { type Recipe, RecipeVisibility } from '../../../../../../shared/models/recipe.data';
import { utcInstant } from '../../../../../../shared/models/semantics/date-value';
import { entityId } from '../../../../../../shared/models/semantics/entity-id';
import { RecipeListResultsComponent } from './recipe-list-results';

describe('RecipeListResultsComponent', () => {
    it('derives visible recipes from recent and all recipe items', () => {
        const { component } = setupComponent({ recentCount: 1, allCount: 0 });

        expect(component['showRecentSection']()).toBe(true);
        expect(component['hasVisibleRecipes']()).toBe(true);
    });

    it('keeps opening and adding independent while retaining the catalogue card', () => {
        const { component, fixture } = setupComponent({ recentCount: 1, allCount: 1 });
        const opened = vi.fn();
        const added = vi.fn();
        component.recipeOpen.subscribe(opened);
        component.recipeAddToMeal.subscribe(added);
        const element = fixture.nativeElement as HTMLElement;
        element.querySelector<HTMLButtonElement>('.recent-recipes__open')?.click();
        expect(opened).toHaveBeenCalledOnce();
        expect(added).not.toHaveBeenCalled();
        element.querySelector<HTMLButtonElement>('.recent-recipes__item fd-ui-button button')?.click();
        expect(added).toHaveBeenCalledOnce();
        expect(opened).toHaveBeenCalledOnce();
        expect(element.querySelectorAll('fd-recipe-card')).toHaveLength(1);
    });

    it('renders empty state when no items and empty state is provided', () => {
        const { fixture } = setupComponent({ recentCount: 0, allCount: 0, emptyState: 'empty' });

        expect(getText(fixture)).toContain('RECIPE_LIST.EMPTY_TITLE');
    });

    it('renders no-results state when no items and no-results state is provided', () => {
        const { fixture } = setupComponent({ recentCount: 0, allCount: 0, emptyState: 'no-results' });

        expect(getText(fixture)).toContain('RECIPE_LIST.NO_RESULTS_TITLE');
    });
});

describe('Recent recipe nutrition in the catalogue', () => {
    it.each([
        { language: 'en', suffix: 'kcal / serving', grouped: '1,234' },
        { language: 'ru', suffix: 'ккал / порция', grouped: '1\u00A0234' },
    ])('distinguishes unknown, zero and known calories in $language', async ({ language, suffix, grouped }) => {
        const { fixture } = setupComponent({ recentCount: 0, allCount: 0 });
        const translate = TestBed.inject(TranslateService);
        translate.setTranslation(language, { RECIPE_LIST: { RECENT_SERVING: `{{calories}} ${suffix}` } }, true);
        await firstValueFrom(translate.use(language));

        const unknown = { ...createRecipe('unknown'), name: 'Uncalculated recipe', totalCalories: null, missingIngredientCount: 1 };
        const zero = { ...createRecipe('zero'), name: 'Zero-calorie recipe', totalCalories: 0 };
        const known = { ...createRecipe('known'), name: 'Calculated recipe', totalCalories: 2468 };
        fixture.componentRef.setInput(
            'recentRecipeItems',
            [unknown, zero, known].map(recipe => ({ recipe, imageUrl: undefined })),
        );
        fixture.detectChanges();

        const host = fixture.nativeElement as HTMLElement;
        const captions = Array.from(host.querySelectorAll('.recent-recipes__copy .fd-ui-meta-text'), element => element.textContent.trim());
        expect(captions).toEqual([`— ${suffix}`, `0 ${suffix}`, `${grouped} ${suffix}`]);
    });
});

describe('Recipe catalog photo preview', () => {
    afterEach(() => {
        vi.unstubAllGlobals();
    });

    it('opens cover and step photos in order and keeps opening recipe details independent', async () => {
        vi.stubGlobal(
            'matchMedia',
            vi.fn(() => ({ matches: false })),
        );
        const cover = { imageAssetId: entityId<'image-asset'>('cover'), imageUrl: '/recipes/cover.jpg' };
        const serving = { imageAssetId: 'serving', imageUrl: '/recipes/serving.jpg' };
        const stepCover = { imageAssetId: 'prep', imageUrl: '/recipes/prep.jpg' };
        const stepPhoto = { imageAssetId: 'mixing', imageUrl: '/recipes/mixing.jpg' };
        const recipe: Recipe = {
            ...createRecipe('recipe-with-photos'),
            visibility: RecipeVisibility.Public,
            name: 'Seasonal salad',
            imageUrl: cover.imageUrl,
            imageAssetId: cover.imageAssetId,
            images: [cover, serving],
            steps: [
                {
                    id: 'mix',
                    stepNumber: 1,
                    instruction: 'Toss the salad.',
                    imageUrl: stepCover.imageUrl,
                    imageAssetId: stepCover.imageAssetId,
                    images: [stepCover, stepPhoto],
                    ingredients: [],
                },
            ],
        };
        const { component, fixture } = setupComponent({ recentCount: 0, allCount: 0 });
        const opened = vi.fn();
        const added = vi.fn();
        component.recipeOpen.subscribe(opened);
        component.recipeAddToMeal.subscribe(added);
        fixture.componentRef.setInput('allRecipeItems', [{ recipe, imageUrl: cover.imageUrl }]);
        fixture.detectChanges();
        const host = fixture.nativeElement as HTMLElement;
        const thumb = previewControl(host, '.entity-card__thumb[role="button"]');
        thumb.dispatchEvent(new KeyboardEvent('keydown', { key: 'Enter', bubbles: true }));
        fixture.detectChanges();
        await fixture.whenStable();
        const pageDocument = TestBed.inject(DOCUMENT);
        const dialog = previewControl(pageDocument, 'fd-ui-image-preview-dialog');
        const imageSelector = '.fd-ui-image-preview-dialog__image';
        expect(dialog.querySelector(imageSelector)?.getAttribute('src')).toBe(cover.imageUrl);
        expect(dialog.querySelector(imageSelector)?.getAttribute('alt')).toBe(recipe.name);
        const thumbnails = dialog.querySelectorAll<HTMLButtonElement>('.fd-ui-image-preview-dialog__thumbnail');
        expect(Array.from(thumbnails, thumbnail => thumbnail.querySelector('img')?.getAttribute('src'))).toEqual([
            cover.imageUrl,
            serving.imageUrl,
            stepCover.imageUrl,
            stepPhoto.imageUrl,
        ]);
        thumbnails[2].click();
        fixture.detectChanges();
        expect(dialog.querySelector(imageSelector)?.getAttribute('src')).toBe(stepCover.imageUrl);
        expect(opened).not.toHaveBeenCalled();
        expect(added).not.toHaveBeenCalled();

        previewControl(dialog, '.fd-ui-dialog__close-button').click();
        fixture.detectChanges();
        await fixture.whenStable();
        expect(pageDocument.querySelector('fd-ui-image-preview-dialog')).toBeNull();
        previewControl(host, '.entity-card__open-button').click();
        expect(opened).toHaveBeenCalledExactlyOnceWith(recipe);
        expect(added).not.toHaveBeenCalled();
    });
});

function previewControl(root: ParentNode, selector: string): HTMLElement {
    const control = root.querySelector(selector);
    if (!(control instanceof HTMLElement)) {
        throw new Error(`Expected preview control ${selector}`);
    }
    return control;
}

function setupComponent(options: { allCount: number; emptyState?: 'empty' | 'no-results'; recentCount: number }): {
    component: RecipeListResultsComponent;
    fixture: ComponentFixture<RecipeListResultsComponent>;
} {
    TestBed.configureTestingModule({
        imports: [RecipeListResultsComponent],
        providers: [provideTranslateTesting()],
    });

    const fixture = TestBed.createComponent(RecipeListResultsComponent);
    fixture.componentRef.setInput('recentRecipeItems', createRecipeItems(options.recentCount));
    fixture.componentRef.setInput('allRecipeItems', createRecipeItems(options.allCount));
    fixture.componentRef.setInput('allRecipesSectionLabelKey', 'RECIPE_LIST.ALL_RECIPES');
    fixture.componentRef.setInput('emptyState', options.emptyState ?? 'empty');
    fixture.componentRef.setInput('favoriteLoadingIds', new Set<string>());
    fixture.detectChanges();

    return { component: fixture.componentInstance, fixture };
}

function getText(fixture: ComponentFixture<RecipeListResultsComponent>): string {
    return (fixture.nativeElement as HTMLElement).textContent;
}

function createRecipeItems(count: number): Array<{ recipe: Recipe; imageUrl: string | undefined }> {
    return Array.from({ length: count }, (_, index) => ({
        recipe: createRecipe(`recipe-${index}`),
        imageUrl: undefined,
    }));
}

function createRecipe(id: string): Recipe {
    return {
        id: entityId<'recipe'>(id),
        name: 'Recipe',
        servings: 2,
        totalCalories: 600,
        visibility: RecipeVisibility.Private,
        usageCount: 0,
        createdAt: utcInstant('2026-01-01T00:00:00Z'),
        isOwnedByCurrentUser: true,
        isNutritionAutoCalculated: true,
        steps: [],
    };
}
