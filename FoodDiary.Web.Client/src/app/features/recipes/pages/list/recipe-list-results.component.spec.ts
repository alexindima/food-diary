import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { TranslateModule } from '@ngx-translate/core';
import { describe, expect, it } from 'vitest';

import { type Recipe, RecipeVisibility } from '../../models/recipe.data';
import { RecipeListResultsComponent } from './recipe-list-results.component';

describe('RecipeListResultsComponent', () => {
    it('derives visible recipes from recent and all recipe items', () => {
        const { component } = setupComponent({ recentCount: 1, allCount: 0 });

        expect(component.showRecentSection()).toBe(true);
        expect(component.hasVisibleRecipes()).toBe(true);
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

function setupComponent(options: { allCount: number; emptyState?: 'empty' | 'no-results'; recentCount: number }): {
    component: RecipeListResultsComponent;
    fixture: ComponentFixture<RecipeListResultsComponent>;
} {
    TestBed.configureTestingModule({
        imports: [RecipeListResultsComponent, TranslateModule.forRoot()],
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
        id,
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
