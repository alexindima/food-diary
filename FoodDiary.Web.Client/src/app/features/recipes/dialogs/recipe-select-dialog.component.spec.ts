import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { FdUiDialogRef } from 'fd-ui-kit/dialog/fd-ui-dialog-ref';
import { of } from 'rxjs';
import { describe, expect, it, vi } from 'vitest';

import { APP_SEARCH_DEBOUNCE_MS } from '../../../config/runtime-ui.tokens';
import type { PageOf } from '../../../shared/models/page-of.data';
import { RecipeService } from '../api/recipe.service';
import { type Recipe, RecipeVisibility } from '../models/recipe.data';
import { RecipeSelectDialogComponent } from './recipe-select-dialog.component';
import type { RecipeSelectItemViewModel } from './recipe-select-dialog.types';

const PAGE_SIZE = 10;
const SECOND_PAGE_INDEX = 1;
const SECOND_PAGE = 2;
const ZERO_DEBOUNCE_MS = 0;

describe('RecipeSelectDialogComponent', () => {
    it('loads recipes on creation and maps them to selectable items', () => {
        const recipe = createRecipe();
        const { component, recipeService } = setupComponent([recipe]);

        expect(recipeService.query).toHaveBeenCalledWith(1, PAGE_SIZE, { search: undefined }, true);
        expect(readRecipeItems(component)).toEqual([{ recipe, imageUrl: 'assets/images/stubs/receipt.png' }]);
    });

    it('closes dialog with selected recipe when used as dialog', () => {
        const recipe = createRecipe();
        const { component, dialogRef } = setupComponent([recipe]);

        component.onRecipeClick(recipe);

        expect(dialogRef.close).toHaveBeenCalledWith(recipe);
    });

    it('emits selected recipe when embedded', () => {
        const recipe = createRecipe();
        const { component, fixture, dialogRef } = setupComponent([recipe]);
        const selected: Recipe[] = [];
        component.recipeSelected.subscribe(value => {
            selected.push(value);
        });
        fixture.componentRef.setInput('embedded', true);
        fixture.detectChanges();

        component.onRecipeClick(recipe);

        expect(dialogRef.close).not.toHaveBeenCalled();
        expect(selected).toEqual([recipe]);
    });

    it('loads requested page and scrolls to top on page change', () => {
        const { component, recipeService } = setupComponent([createRecipe()]);
        const scrollSpy = vi.spyOn(component as unknown as { scrollToTop: () => void }, 'scrollToTop').mockImplementation(() => undefined);

        component.onPageChange(SECOND_PAGE_INDEX);

        expect(scrollSpy).toHaveBeenCalled();
        expect(recipeService.query).toHaveBeenLastCalledWith(SECOND_PAGE, PAGE_SIZE, { search: undefined }, true);
    });

    it('toggles only-mine filter through form control', () => {
        const { component } = setupComponent([createRecipe()]);

        component.toggleOnlyMine();

        expect(component.searchForm.controls.onlyMine.value).toBe(true);
        expect(component.onlyMineFilter()).toBe(true);
    });
});

function setupComponent(recipes: Recipe[]): {
    component: RecipeSelectDialogComponent;
    dialogRef: { close: ReturnType<typeof vi.fn> };
    fixture: ComponentFixture<RecipeSelectDialogComponent>;
    recipeService: { query: ReturnType<typeof vi.fn> };
} {
    const recipeService = {
        query: vi.fn().mockReturnValue(of(createPage(recipes))),
    };
    const dialogRef = { close: vi.fn() };

    TestBed.configureTestingModule({
        imports: [RecipeSelectDialogComponent],
        providers: [
            { provide: RecipeService, useValue: recipeService },
            { provide: FdUiDialogRef, useValue: dialogRef },
            { provide: APP_SEARCH_DEBOUNCE_MS, useValue: ZERO_DEBOUNCE_MS },
        ],
    });
    TestBed.overrideComponent(RecipeSelectDialogComponent, {
        set: {
            template: '<div #container></div>',
        },
    });

    const fixture = TestBed.createComponent(RecipeSelectDialogComponent);
    fixture.detectChanges();

    return { component: fixture.componentInstance, dialogRef, fixture, recipeService };
}

function readRecipeItems(component: RecipeSelectDialogComponent): RecipeSelectItemViewModel[] {
    return (component as unknown as { recipeItems: () => RecipeSelectItemViewModel[] }).recipeItems();
}

function createPage(data: Recipe[]): PageOf<Recipe> {
    return {
        data,
        page: 1,
        limit: PAGE_SIZE,
        totalPages: 1,
        totalItems: data.length,
    };
}

function createRecipe(): Recipe {
    return {
        id: 'recipe-1',
        name: 'Recipe',
        imageUrl: null,
        servings: 2,
        visibility: RecipeVisibility.Private,
        usageCount: 0,
        createdAt: '2026-01-01T00:00:00Z',
        isOwnedByCurrentUser: true,
        isNutritionAutoCalculated: true,
        steps: [],
    };
}
