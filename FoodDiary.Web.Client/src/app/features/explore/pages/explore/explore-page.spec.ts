import type { ComponentFixture } from '@angular/core/testing';
import { TestBed } from '@angular/core/testing';
import { FdTourService } from 'fd-tour';
import { FdUiDialogService } from 'fd-ui-kit/dialog/fd-ui-dialog.service';
import { of, Subject } from 'rxjs';
import { beforeEach, describe, expect, it, vi } from 'vitest';

import { provideTranslateTesting } from '../../../../../testing/translate-testing.module';
import { EXPLORE_SEARCH_DEBOUNCE_MS } from '../../../../config/runtime-ui.tokens';
import type { PageOf } from '../../../../shared/models/page-of.data';
import { type Recipe, RecipeVisibility } from '../../../../shared/models/recipe.data';
import { utcInstant } from '../../../../shared/models/semantics/date-value';
import { entityId } from '../../../../shared/models/semantics/entity-id';
import { RecipeDetailComponent } from '../../../recipes/contracts/recipe-detail';
import { ExploreInteractionsFacade } from '../../lib/explore-interactions.facade';
import type { ExploreRecipe } from '../../models/explore.data';
import { ExplorePageComponent } from './explore-page';
import { EXPLORE_PAGE_SIZE } from './explore-page-lib/explore-page.constants';

const TOTAL_ITEMS = EXPLORE_PAGE_SIZE * 2 + 1;
const TOTAL_PAGES = 3;
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

    it('loads selected page using one-based API page number', () => {
        component['onPageChange'](SELECTED_PAGE_INDEX);

        expect(component['currentPageIndex']()).toBe(SELECTED_PAGE_INDEX);
        expect(exploreService.queryRecipes).toHaveBeenLastCalledWith(SELECTED_API_PAGE, EXPLORE_PAGE_SIZE, {
            search: '',
            sortBy: 'newest',
        });
    });

    it('restarts pagination on a search and keeps the matching recipes after a late page response', async () => {
        const previousPage = new Subject<PageOf<ExploreRecipe>>();
        const matchingPage = new Subject<PageOf<ExploreRecipe>>();
        exploreService.queryRecipes.mockReturnValueOnce(previousPage).mockReturnValueOnce(matchingPage);
        const host = fixture.nativeElement as HTMLElement;
        const nextPage = host.querySelector<HTMLButtonElement>('fd-ui-pagination button:last-child');
        const search = host.querySelector<HTMLInputElement>('.explore-search input');
        if (nextPage === null || search === null) {
            throw new Error('Expected recipe pagination and search controls');
        }

        nextPage.click();
        expect(exploreService.queryRecipes).toHaveBeenLastCalledWith(2, EXPLORE_PAGE_SIZE, { search: '', sortBy: 'newest' });

        search.value = 'ramen';
        search.dispatchEvent(new Event('input', { bubbles: true }));
        await vi.waitFor(() => {
            fixture.detectChanges();
            expect(exploreService.queryRecipes).toHaveBeenLastCalledWith(1, EXPLORE_PAGE_SIZE, { search: 'ramen', sortBy: 'newest' });
        });
        expect(host.querySelector('.explore-loader')).not.toBeNull();
        expect(previousPage.observed).toBe(false);

        const matchingRecipe = { ...createRecipe(), id: entityId<'recipe'>('ramen'), name: 'Miso ramen' };
        matchingPage.next({ ...createPage(), data: [matchingRecipe], totalPages: 1, totalItems: 1 });
        matchingPage.complete();
        previousPage.next({ ...createPage(), page: 2 });
        previousPage.complete();
        fixture.detectChanges();

        const card = host.querySelector<HTMLElement>('.explore-grid .entity-card');
        expect(card?.textContent).toContain('Miso ramen');
        expect(host.querySelector('.explore-loader')).toBeNull();
        expect(host.querySelector('fd-ui-pagination')).toBeNull();
        card?.click();
        expect(dialogService.open).toHaveBeenCalledExactlyOnceWith(RecipeDetailComponent, { preset: 'detail', data: matchingRecipe });
    });

    it('replays the Explore tour from the rendered help button', () => {
        const start = vi.spyOn(TestBed.inject(FdTourService), 'start').mockReturnValue(true);
        const host = fixture.nativeElement as HTMLElement;
        host.querySelector<HTMLButtonElement>('[data-tour-id="explore-tour-help"] button')?.click();

        expect(start).toHaveBeenCalledExactlyOnceWith(expect.objectContaining({ id: 'explore' }), { force: true });
    });
});

describe('Explore rendered sort controls', () => {
    it('marks the active sort choice and returns to the first page when switching sort order', () => {
        const host = fixture.nativeElement as HTMLElement;
        const sortButtons = Array.from(host.querySelectorAll<HTMLButtonElement>('.explore-sort button'));
        const newest = sortButtons.find(button => button.textContent.trim() === 'EXPLORE.SORT_NEWEST');
        const popular = sortButtons.find(button => button.textContent.trim() === 'EXPLORE.SORT_POPULAR');
        const nextPage = host.querySelector<HTMLButtonElement>('fd-ui-pagination button:last-child');
        if (newest === undefined || popular === undefined || nextPage === null) {
            throw new Error('Expected Explore sort buttons and pagination');
        }
        expect(newest.classList).toContain('fd-ui-button--primary');
        expect(popular.classList).toContain('fd-ui-button--outline');

        const olderRecipe = { ...createRecipe(), id: entityId<'recipe'>('older-recipe'), name: 'Second page stew' };
        const popularRecipe = { ...createRecipe(), id: entityId<'recipe'>('popular-recipe'), name: 'Most liked ramen' };
        exploreService.queryRecipes
            .mockReturnValueOnce(of({ ...createPage(), page: 2, data: [olderRecipe] }))
            .mockReturnValueOnce(of({ ...createPage(), data: [popularRecipe] }))
            .mockReturnValueOnce(of(createPage()));
        nextPage.click();
        fixture.detectChanges();
        expect(exploreService.queryRecipes).toHaveBeenLastCalledWith(2, EXPLORE_PAGE_SIZE, { search: '', sortBy: 'newest' });
        expect(host.querySelector('.explore-grid')?.textContent).toContain(olderRecipe.name);

        popular.click();
        fixture.detectChanges();
        expect(exploreService.queryRecipes).toHaveBeenLastCalledWith(1, EXPLORE_PAGE_SIZE, { search: '', sortBy: 'popular' });
        expect(newest.classList).toContain('fd-ui-button--outline');
        expect(popular.classList).toContain('fd-ui-button--primary');
        expect(host.querySelector('.explore-grid')?.textContent).toContain(popularRecipe.name);

        newest.click();
        fixture.detectChanges();
        expect(exploreService.queryRecipes).toHaveBeenLastCalledWith(1, EXPLORE_PAGE_SIZE, { search: '', sortBy: 'newest' });
        expect(newest.classList).toContain('fd-ui-button--primary');
        expect(popular.classList).toContain('fd-ui-button--outline');
        expect(host.querySelector('.explore-grid')?.textContent).toContain(createRecipe().name);
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
        id: entityId<'recipe'>('recipe-1'),
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
        createdAt: utcInstant('2026-05-16T10:00:00.000Z'),
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
