import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { FdUiDialogService } from 'fd-ui-kit/dialog/fd-ui-dialog.service';
import { FdUiImagePreviewDialogComponent } from 'fd-ui-kit/image-preview-dialog/fd-ui-image-preview-dialog';
import { describe, expect, it, vi } from 'vitest';

import { provideTranslateTesting } from '../../../../../../testing/translate-testing.module';
import { type Recipe, RecipeVisibility } from '../../../../../shared/models/recipe.data';
import { RecipeCookModeComponent } from './recipe-cook-mode';

describe('RecipeCookModeComponent', () => {
    it('opens the selected step photo with the entire gallery and clears photos when advancing', () => {
        const recipe = createRecipe();
        recipe.steps[0].images = [
            { imageAssetId: 'photo-1', imageUrl: '/first.jpg' },
            { imageAssetId: 'photo-2', imageUrl: '/second.jpg' },
        ];
        const { component, fixture, open } = setupComponent(recipe);
        const root = fixture.nativeElement as HTMLElement;
        const photos = root.querySelectorAll<HTMLButtonElement>('.recipe-cook-mode__photo');
        expect(photos).toHaveLength(2);
        photos[1].click();
        expect(open).toHaveBeenCalledExactlyOnceWith(FdUiImagePreviewDialogComponent, {
            size: 'lg',
            data: { collageImages: [{ url: '/first.jpg' }, { url: '/second.jpg' }], initialIndex: 1 },
        });
        component['nextStep']();
        fixture.detectChanges();
        expect(root.querySelectorAll('.recipe-cook-mode__photo')).toHaveLength(0);
    });

    it('retains the legacy step photo when no gallery was stored', () => {
        const recipe = createRecipe();
        recipe.steps[0].images = [];
        recipe.steps[0].imageUrl = '/legacy.jpg';
        const { fixture, open } = setupComponent(recipe);
        const photo = (fixture.nativeElement as HTMLElement).querySelector<HTMLButtonElement>('.recipe-cook-mode__photo');
        expect(photo?.querySelector('img')?.getAttribute('src')).toBe('/legacy.jpg');
        photo?.click();
        expect(open).toHaveBeenCalledExactlyOnceWith(FdUiImagePreviewDialogComponent, {
            size: 'lg',
            data: { collageImages: [{ url: '/legacy.jpg' }], initialIndex: 0 },
        });
    });

    it('steps through recipe instructions', () => {
        const { component, fixture } = setupComponent(createRecipe());

        expect(component['currentStep']()?.instruction).toBe('Mix');
        expect(component['canGoBack']()).toBe(false);
        expect(component['canGoNext']()).toBe(true);

        component['nextStep']();
        fixture.detectChanges();

        expect(component['currentStep']()?.instruction).toBe('Bake');
        expect(component['canGoBack']()).toBe(true);
        expect(component['canGoNext']()).toBe(false);
        expect(component['isDone']()).toBe(true);
    });

    it('removes the ingredient section for a step without ingredients', () => {
        const { component, fixture } = setupComponent(createRecipe());
        expect((fixture.nativeElement as HTMLElement).querySelector('.recipe-cook-mode__ingredients')).not.toBeNull();
        component['nextStep']();
        fixture.detectChanges();
        expect((fixture.nativeElement as HTMLElement).querySelector('.recipe-cook-mode__ingredients')).toBeNull();
    });

    it('shows portions for a nested recipe', () => {
        const recipe = createRecipe();
        recipe.steps[0].ingredients = [{ id: 'nested', nestedRecipeId: 'recipe-2', nestedRecipeName: 'Sauce', amount: 0.5 }];
        const { component } = setupComponent(recipe);
        expect(component['ingredients']()[0].unitKey).toBe('RECIPE_DETAIL.SUMMARY.SERVINGS_FEW');
    });

    it('builds current-step ingredient views', () => {
        const { component } = setupComponent(createRecipe());

        expect(component['ingredients']()).toEqual([
            {
                name: 'Flour',
                amount: 100,
                unitKey: 'GENERAL.UNITS.G',
            },
        ]);
    });
});

function setupComponent(recipe: Recipe): {
    component: RecipeCookModeComponent;
    fixture: ComponentFixture<RecipeCookModeComponent>;
    open: ReturnType<typeof vi.fn>;
} {
    const open = vi.fn();
    TestBed.configureTestingModule({
        imports: [RecipeCookModeComponent],
        providers: [provideTranslateTesting(), { provide: FdUiDialogService, useValue: { open } }],
    });

    const fixture = TestBed.createComponent(RecipeCookModeComponent);
    fixture.componentRef.setInput('recipe', recipe);
    fixture.detectChanges();

    return { component: fixture.componentInstance, fixture, open };
}

function createRecipe(): Recipe {
    return {
        id: 'recipe-1',
        name: 'Cake',
        description: null,
        comment: null,
        category: null,
        imageUrl: null,
        imageAssetId: null,
        prepTime: null,
        cookTime: null,
        servings: 4,
        visibility: RecipeVisibility.Public,
        usageCount: 0,
        createdAt: '2026-01-01T00:00:00Z',
        isOwnedByCurrentUser: true,
        totalCalories: null,
        totalProteins: null,
        totalFats: null,
        totalCarbs: null,
        totalFiber: null,
        totalAlcohol: null,
        isNutritionAutoCalculated: true,
        steps: [
            {
                id: 'step-1',
                stepNumber: 1,
                title: null,
                instruction: 'Mix',
                imageUrl: null,
                imageAssetId: null,
                ingredients: [
                    {
                        id: 'ingredient-1',
                        amount: 100,
                        productId: 'product-1',
                        productName: 'Flour',
                        productBaseUnit: 'G',
                    },
                ],
            },
            {
                id: 'step-2',
                stepNumber: 2,
                title: null,
                instruction: 'Bake',
                imageUrl: null,
                imageAssetId: null,
                ingredients: [],
            },
        ],
    };
}

describe('RecipeCookModeComponent boundaries', () => {
    it('handles a recipe without instructions without allowing navigation', () => {
        const { component } = setupComponent({ ...createRecipe(), steps: [] });
        component['previousStep']();
        component['nextStep']();
        expect(component['currentStep']()).toBeNull();
        expect(component['ingredients']()).toEqual([]);
        expect(component['progressPercent']()).toBe(0);
        expect(component['isDone']()).toBe(false);
    });

    it('sorts steps without mutating the recipe and does not move beyond boundaries', () => {
        const recipe = createRecipe();
        recipe.steps.reverse();
        const { component } = setupComponent(recipe);
        expect(recipe.steps[0].instruction).toBe('Bake');
        expect(component['currentStep']()?.instruction).toBe('Mix');
        component['previousStep']();
        expect(component['currentStepIndex']()).toBe(0);
        component['nextStep']();
        component['nextStep']();
        expect(component['currentStepIndex']()).toBe(1);
        component['previousStep']();
        expect(component['currentStepIndex']()).toBe(0);
    });

    it('uses nested recipe names and omits unknown measurement units', () => {
        const recipe = createRecipe();
        recipe.steps[0].ingredients = [
            { ...recipe.steps[0].ingredients[0], productName: null, nestedRecipeName: 'Sauce', productBaseUnit: null },
        ];
        const { component } = setupComponent(recipe);
        expect(component['ingredients']()[0]).toMatchObject({ name: 'Sauce', unitKey: '' });
    });
});
