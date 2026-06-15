import type { ComponentFixture } from '@angular/core/testing';
import { TestBed } from '@angular/core/testing';
import { FdUiDialogService } from 'fd-ui-kit/dialog/fd-ui-dialog.service';
import { of } from 'rxjs';
import { beforeEach, describe, expect, it, vi } from 'vitest';

import { provideTranslateTesting } from '../../../../../testing/translate-testing.module';
import { EXPLORE_SEARCH_DEBOUNCE_MS } from '../../../../config/runtime-ui.tokens';
import type { PageOf } from '../../../../shared/models/page-of.data';
import { type Recipe, RecipeVisibility } from '../../../recipes/models/recipe.data';
import { ExploreInteractionsFacade } from '../../lib/explore-interactions.facade';
import type { ExploreRecipe } from '../../models/explore.data';
import { ExplorePageComponent } from './explore-page';
import { EXPLORE_PAGE_SIZE } from './explore-page-lib/explore-page.constants';

const TOTAL_ITEMS = 1;
const TOTAL_PAGES = 1;
const RECIPE_SERVINGS = 2;
const RECIPE_CALORIES = 350;
const SELECTED_PAGE_INDEX = 2;
const SELECTED_API_PAGE = 3;

let fixture: ComponentFixture<ExplorePageComponent>;
let component: ExplorePageComponent;
let exploreService: { queryRecipes: ReturnType<typeof vi.fn> };
let dialogService: { open: ReturnType<typeof vi.fn> };

beforeEach(() => {
    exploreService = {
        queryRecipes: vi.fn(() => of(createPage())),
    };
    dialogService = {
        open: vi.fn(),
    };

    TestBed.configureTestingModule({
        imports: [ExplorePageComponent],
        providers: [
            provideTranslateTesting(),
            { provide: ExploreInteractionsFacade, useValue: exploreService },
            { provide: EXPLORE_SEARCH_DEBOUNCE_MS, useValue: 0 },
            { provide: FdUiDialogService, useValue: dialogService },
        ],
    });

    fixture = TestBed.createComponent(ExplorePageComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
});

describe('ExplorePageComponent', () => {
    it('loads newest recipes on init', () => {
        expect(exploreService.queryRecipes).toHaveBeenCalledWith(1, EXPLORE_PAGE_SIZE, { search: '', sortBy: 'newest' });
        expect(component['recipeData'].items()).toEqual([createRecipe()]);
    });

    it('reloads first page when sort changes', () => {
        component['onSortChange']('popular');

        expect(component['sortBy']()).toBe('popular');
        expect(component['currentPageIndex']()).toBe(0);
        expect(exploreService.queryRecipes).toHaveBeenLastCalledWith(1, EXPLORE_PAGE_SIZE, { search: '', sortBy: 'popular' });
    });

    it('loads selected page using one-based API page number', () => {
        component['onPageChange'](SELECTED_PAGE_INDEX);

        expect(component['currentPageIndex']()).toBe(SELECTED_PAGE_INDEX);
        expect(exploreService.queryRecipes).toHaveBeenLastCalledWith(SELECTED_API_PAGE, EXPLORE_PAGE_SIZE, {
            search: '',
            sortBy: 'newest',
        });
    });
});

function createPage(): PageOf<ExploreRecipe> {
    return {
        data: [createRecipe()],
        page: 1,
        limit: EXPLORE_PAGE_SIZE,
        totalPages: TOTAL_PAGES,
        totalItems: TOTAL_ITEMS,
    };
}

function createRecipe(): Recipe {
    return {
        id: 'recipe-1',
        name: 'Soup',
        description: null,
        comment: null,
        category: null,
        imageUrl: null,
        imageAssetId: null,
        prepTime: null,
        cookTime: null,
        servings: RECIPE_SERVINGS,
        visibility: RecipeVisibility.Public,
        usageCount: 0,
        createdAt: '2026-05-16T10:00:00.000Z',
        isOwnedByCurrentUser: false,
        totalCalories: RECIPE_CALORIES,
        totalProteins: 0,
        totalFats: 0,
        totalCarbs: 0,
        totalFiber: 0,
        totalAlcohol: 0,
        isNutritionAutoCalculated: true,
        steps: [],
    };
}
