import { computed, DestroyRef, inject, Injectable, signal } from '@angular/core';
import { rxResource, takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { TranslateService } from '@ngx-translate/core';
import { FdUiDialogService } from 'fd-ui-kit/dialog/fd-ui-dialog.service';
import { FdUiToastService } from 'fd-ui-kit/toast/fd-ui-toast.service';
import {
    catchError,
    defaultIfEmpty,
    defer,
    EMPTY,
    filter,
    finalize,
    firstValueFrom,
    fromEvent,
    map,
    type Observable,
    of,
    Subject,
    switchMap,
    take,
    takeUntil,
    tap,
} from 'rxjs';

import { NavigationService } from '../../../services/navigation.service';
import { developmentRequestContext } from '../../../shared/development/development-request-context';
import { PagedData } from '../../../shared/lib/paged-data.data';
import { RequestStateController } from '../../../shared/lib/request-state';
import type { FavoriteRecipe, Recipe, RecipeFilters, RecipeOverview } from '../../../shared/models/recipe.data';
import type { FavoriteRecipeId, RecipeId } from '../../../shared/models/semantics/entity-id';
import { resolvePaginationPage } from '../../../shared/navigation/pagination-query.utils';
import { QuickMealService } from '../../meals/contracts/quick-meal';
import { FavoriteRecipeService } from '../api/favorite-recipe.service';
import { RecipeService } from '../api/recipe.service';
import type { RecipeDetailActionResult } from '../components/detail/recipe-detail-lib/recipe-detail.types';
import {
    RECIPE_LIST_OVERVIEW_FAVORITE_LIMIT,
    RECIPE_LIST_OVERVIEW_RECENT_LIMIT,
    RECIPE_LIST_PAGE_SIZE,
} from '../components/list/recipe-list.config';
import { RecipeListFiltersDialogComponent } from '../components/list/recipe-list-filters-dialog/recipe-list-filters-dialog';
import type { RecipeListFiltersDialogResult } from '../components/list/recipe-list-filters-dialog/recipe-list-filters-dialog.types';
import { createRecipeListQuery, type RecipeListQuery, recipeListQueryKey, recipeQueryFilters } from './list/recipe-list-query';
import { RECIPE_LIST_QUERY_STATE } from './list/recipe-list-query-state';

type RecipeListRequest = {
    page: number;
    limit: number;
    filters: RecipeFilters;
    onlyMine: boolean;
    initial?: boolean;
};

type RecipeOverviewRequest = {
    query: RecipeListQuery;
    limit: number;
    filters: RecipeFilters;
    initial: boolean;
    requestId: number;
};

@Injectable()
export class RecipeListFacade {
    private readonly destroyRef = inject(DestroyRef);
    private readonly cancelLoad = new Subject<void>();
    private readonly recipeService = inject(RecipeService);
    private readonly navigationService = inject(NavigationService);
    private readonly translateService = inject(TranslateService);
    private readonly toastService = inject(FdUiToastService);
    private readonly quickMealService = inject(QuickMealService);
    private readonly favoriteRecipeService = inject(FavoriteRecipeService);
    private readonly dialogService = inject(FdUiDialogService);
    private readonly queryState = inject(RECIPE_LIST_QUERY_STATE, { optional: true });
    private activeRouteQuery: RecipeListQuery | null = null;
    private readonly appliedRequest = new RequestStateController<void>();
    private favoriteCountRevision = 0;
    private favoriteOverviewReadId: number | null = null;
    private readonly requestedView = signal<RecipeOverviewRequest | undefined>(undefined);
    private readonly viewResource = rxResource({
        params: this.requestedView,
        stream: ({ params }) => this.readOverview(params).pipe(defaultIfEmpty(void 0)),
    });
    public readonly requestProvenance = this.appliedRequest.provenance;

    public readonly pageSize = RECIPE_LIST_PAGE_SIZE;
    public readonly recipeData = new PagedData<Recipe>();
    public readonly currentPageIndex = signal(0);
    public readonly recentRecipes = signal<Recipe[]>([]);
    public readonly favoriteRecipes = signal<FavoriteRecipe[]>([]);
    public readonly favoriteTotalCount = signal(0);
    public readonly errorKey = signal<string | null>(null);
    public readonly isDeleting = signal(false);
    public readonly isFavoritesLoadingMore = signal(false);
    public readonly favoriteLoadingIds = signal<ReadonlySet<string>>(new Set<string>());

    private readonly filtersActive = signal(false);
    public readonly showRecentSection = computed(
        () =>
            this.currentPageIndex() === 0 &&
            !this.filtersActive() &&
            !this.hasSearchValue(this.searchValue()) &&
            this.recentRecipes().length > 0,
    );
    public readonly allRecipesSectionItems = computed(() => this.recipeData.items());
    public readonly hasVisibleRecipes = computed(() => this.showRecentSection() || this.allRecipesSectionItems().length > 0);
    public readonly allRecipesSectionLabelKey = computed(() =>
        this.hasSearchValue(this.searchValue()) ? 'RECIPE_LIST.SEARCH_RESULTS' : 'RECIPE_LIST.ALL_RECIPES',
    );

    private readonly searchValue = signal<string | null>(null);

    public constructor() {
        if (this.queryState !== null) {
            this.applyRouteQuery(this.queryState.initial);
            this.queryState.changes
                .pipe(
                    filter(query => !this.matchesActiveRouteQuery(query)),
                    takeUntilDestroyed(this.destroyRef),
                )
                .subscribe(query => {
                    this.applyRouteQuery(query);
                    this.requestView({
                        page: query.page,
                        limit: this.pageSize,
                        filters: recipeQueryFilters(query),
                        onlyMine: query.onlyMine,
                    });
                });
        }
    }

    public requestView(request: RecipeListRequest): void {
        const overview = this.prepareOverview(request);
        this.viewResource.set(undefined);
        this.requestedView.set(overview);
    }

    public isQueryActive(filters: RecipeFilters, onlyMine: boolean): boolean {
        return (
            this.activeRouteQuery !== null &&
            recipeListQueryKey(this.activeRouteQuery) ===
                recipeListQueryKey(createRecipeListQuery(this.activeRouteQuery.page, filters, onlyMine))
        );
    }

    public openFilters(data: RecipeListFilterDialogData): Observable<RecipeListFiltersDialogResult | null | undefined> {
        return this.dialogService
            .open<RecipeListFiltersDialogComponent, RecipeListFilterDialogData, RecipeListFiltersDialogResult | null>(
                RecipeListFiltersDialogComponent,
                { preset: 'form', data },
            )
            .afterClosed();
    }

    public loadRecipes(page: number, limit: number, filters: RecipeFilters, onlyMine: boolean): Observable<void> {
        return this.loadOverview({ page, limit, filters, onlyMine });
    }

    public loadInitialOverview(page: number, limit: number, filters: RecipeFilters, onlyMine: boolean): Observable<void> {
        return this.loadOverview({ page, limit, filters, onlyMine, initial: true });
    }

    private loadOverview(request: RecipeListRequest): Observable<void> {
        const overview = this.prepareOverview(request);
        return overview === undefined ? EMPTY : this.readOverview(overview);
    }

    private prepareOverview({ page, limit, filters, onlyMine, initial = false }: RecipeListRequest): RecipeOverviewRequest | undefined {
        const query = createRecipeListQuery(page, filters, onlyMine);
        if (!initial && this.queryState !== null && !this.matchesActiveRouteQuery(query)) {
            this.writeRouteQuery(query);
            return undefined;
        }

        const filtersActive = this.hasActiveFilters(onlyMine, filters);
        const includeInitialSections = initial || (page === 1 && !this.hasSearchValue(filters.search ?? null) && !filtersActive);
        this.filtersActive.set(filtersActive);
        const requestId = this.appliedRequest.begin({ showLoading: false });
        this.cancelLoad.next();
        this.currentPageIndex.set(query.page - 1);
        this.recipeData.setLoading(true);
        this.searchValue.set(filters.search ?? null);

        return { query, limit, filters: { ...filters }, initial: includeInitialSections, requestId };
    }

    private readOverview({ query, limit, filters, initial, requestId }: RecipeOverviewRequest): Observable<void> {
        if (!this.appliedRequest.isCurrent(requestId)) {
            return EMPTY;
        }

        let recovering = false;
        const favoriteCountRevision = this.favoriteCountRevision;
        this.favoriteOverviewReadId = requestId;
        return this.recipeService
            .queryOverview(
                {
                    page: query.page,
                    limit,
                    filters,
                    includePublic: !query.onlyMine,
                    recentLimit: initial ? RECIPE_LIST_OVERVIEW_RECENT_LIMIT : 1,
                    favoriteLimit: initial ? RECIPE_LIST_OVERVIEW_FAVORITE_LIMIT : 0,
                },
                developmentRequestContext(this.appliedRequest.bindRequest(requestId)),
            )
            .pipe(
                takeUntil(this.cancelLoad),
                takeUntilDestroyed(this.destroyRef),
                tap(data => {
                    if (!this.isCurrentRequest(requestId, query)) {
                        return;
                    }
                    if (favoriteCountRevision === this.favoriteCountRevision) {
                        this.favoriteCountRevision++;
                        this.isFavoritesLoadingMore.set(false);
                        this.favoriteTotalCount.set(data.favoriteTotalCount);
                    }
                    if (initial) {
                        this.favoriteRecipes.set(data.favoriteItems);
                    }
                    const page = resolvePaginationPage(data.allRecipes.page, data.allRecipes.totalPages);
                    if (page !== data.allRecipes.page) {
                        recovering = true;
                        this.recoverPage({ ...query, page }, filters);
                        return;
                    }
                    if (this.queryState !== null) {
                        this.writeRouteQuery(query, true);
                    }
                    this.acceptOverview(data, initial);
                    this.appliedRequest.succeed(requestId, undefined);
                }),
                map(() => void 0),
                catchError((_error: unknown) => {
                    if (!this.isCurrentRequest(requestId, query)) {
                        return of(void 0);
                    }
                    this.recipeData.clearData();
                    this.recentRecipes.set([]);
                    if (initial) {
                        this.favoriteRecipes.set([]);
                        if (favoriteCountRevision === this.favoriteCountRevision) {
                            this.favoriteTotalCount.set(0);
                        }
                    }
                    this.errorKey.set('ERRORS.LOAD_FAILED_TITLE');
                    return of(void 0);
                }),
                finalize(() => {
                    if (this.favoriteOverviewReadId === requestId) {
                        this.favoriteOverviewReadId = null;
                    }
                    if (!recovering && this.appliedRequest.isCurrent(requestId)) {
                        this.recipeData.setLoading(false);
                    }
                }),
            );
    }

    private acceptOverview(data: RecipeOverview, initial: boolean): void {
        this.recipeData.setData(data.allRecipes);
        this.recentRecipes.set(initial ? data.recentItems : []);
        this.currentPageIndex.set(data.allRecipes.page - 1);
        this.errorKey.set(null);
    }

    private recoverPage(query: RecipeListQuery, filters: RecipeFilters): void {
        if (this.queryState !== null) {
            this.writeRouteQuery(query, true);
        } else {
            this.requestView({ page: query.page, limit: this.pageSize, filters, onlyMine: query.onlyMine });
        }
    }

    private isCurrentRequest(requestId: number, query: RecipeListQuery): boolean {
        return this.appliedRequest.isCurrent(requestId) && (this.queryState === null || this.queryState.isCurrent(query));
    }

    private writeRouteQuery(query: RecipeListQuery, replaceUrl = false): void {
        void this.queryState?.writeAsync(query, { replaceUrl }).catch(() => {
            this.errorKey.set('ERRORS.LOAD_FAILED_TITLE');
            this.recipeData.setLoading(false);
        });
    }

    private applyRouteQuery(query: RecipeListQuery): void {
        this.activeRouteQuery = query;
        this.currentPageIndex.set(query.page - 1);
    }

    private matchesActiveRouteQuery(query: RecipeListQuery): boolean {
        return this.activeRouteQuery !== null && recipeListQueryKey(query) === recipeListQueryKey(this.activeRouteQuery);
    }

    public async navigateToAddRecipeAsync(): Promise<void> {
        await this.navigationService.navigateToRecipeAddAsync();
    }

    public async navigateToEditRecipeAsync(recipeId: string): Promise<void> {
        await this.navigationService.navigateToRecipeEditAsync(recipeId);
    }

    public async handleDetailActionAsync(
        result: RecipeDetailActionResult,
        recipe: Recipe,
        search: string | null,
        onlyMine: boolean,
    ): Promise<void> {
        if (result.action === 'Edit') {
            await this.navigateToEditRecipeAsync(recipe.id);
            return;
        }

        if (result.action === 'Duplicate') {
            await this.navigateToEditRecipeAsync(result.id);
            return;
        }

        if (result.action === 'AddToMeal') {
            this.addToMeal(recipe);
            return;
        }

        if (result.action === 'Delete') {
            this.deleteRecipe(recipe, search, onlyMine).subscribe();
        }
    }

    public deleteRecipe(recipe: Recipe, search: string | null, onlyMine: boolean): Observable<void> {
        if (!recipe.isOwnedByCurrentUser || recipe.usageCount > 0 || this.isDeleting()) {
            return of(void 0);
        }

        this.isDeleting.set(true);
        this.recipeData.setLoading(true);

        return this.recipeService.deleteById(recipe.id).pipe(
            switchMap(() => {
                if (this.queryState === null) {
                    return this.loadRecipes(1, this.pageSize, { search }, onlyMine);
                }

                const query = this.queryState.current();
                return this.loadRecipes(1, this.pageSize, recipeQueryFilters(query), query.onlyMine);
            }),
            catchError(() => {
                this.toastService.error(this.translateService.instant('RECIPE_LIST.DELETE_ERROR'));
                return of(void 0);
            }),
            finalize(() => {
                this.isDeleting.set(false);
                this.recipeData.setLoading(false);
            }),
        );
    }

    public addToMeal(recipe: Recipe): void {
        this.quickMealService.addRecipe(recipe);
    }

    public loadFavorites(): Observable<void> {
        return defer(() => {
            const revision = ++this.favoriteCountRevision;
            this.isFavoritesLoadingMore.set(true);
            return this.favoriteRecipeService.getPage(1, 1).pipe(
                take(1),
                tap(result => {
                    if (revision === this.favoriteCountRevision) {
                        this.favoriteTotalCount.set(result.totalItems);
                    }
                }),
                map(() => void 0),
                catchError(() => of(void 0)),
                takeUntilDestroyed(this.destroyRef),
                finalize(() => {
                    if (revision === this.favoriteCountRevision) {
                        this.isFavoritesLoadingMore.set(false);
                    }
                }),
            );
        });
    }

    public toggleRecipeFavorite(recipe: Recipe): Observable<void> {
        if (this.favoriteLoadingIds().has(recipe.id)) {
            return of(void 0);
        }

        this.setFavoriteLoading(recipe.id, true);

        if (recipe.isFavorite === true) {
            return this.removeRecipeFavorite(recipe);
        }

        return this.favoriteRecipeService.add(recipe.id, recipe.name).pipe(
            tap(favorite => {
                this.syncRecipeFavoriteState(recipe.id, true, favorite.id);
            }),
            switchMap(() => this.loadFavorites()),
            catchError(() => {
                this.toastService.error(this.translateService.instant('RECIPE_LIST.FAVORITE_ERROR'));
                return of(void 0);
            }),
            finalize(() => {
                this.setFavoriteLoading(recipe.id, false);
            }),
        );
    }

    public getFavoriteRecipe(favorite: FavoriteRecipe): Observable<Recipe | null> {
        return this.recipeService.getById(favorite.recipeId);
    }

    public async getRecipeDetailAsync(recipeId: RecipeId, request: AbortSignal): Promise<Recipe | null> {
        const recipe = await firstValueFrom(
            this.recipeService.getById(recipeId, true).pipe(
                takeUntilDestroyed(this.destroyRef),
                takeUntil(fromEvent(request, 'abort')),
                catchError(() => of(null)),
            ),
            { defaultValue: null },
        );
        if (this.destroyRef.destroyed || request.aborted) {
            return null;
        }
        if (recipe === null) {
            this.toastService.error(this.translateService.instant('ERRORS.LOAD_FAILED_MESSAGE'));
        }
        return recipe;
    }

    public removeFavorite(favorite: FavoriteRecipe): Observable<void> {
        return this.favoriteRecipeService.remove(favorite.id).pipe(
            tap(() => {
                this.favoriteRecipes.update(favorites => favorites.filter(item => item.id !== favorite.id));
                this.syncRecipeFavoriteState(favorite.recipeId, false, null);
                this.applyFavoriteCountDelta(-1);
            }),
            map(() => void 0),
        );
    }

    public addFavoriteToMeal(favorite: FavoriteRecipe): Observable<boolean> {
        return this.recipeService.getById(favorite.recipeId).pipe(
            map(recipe => {
                if (recipe === null) {
                    return false;
                }
                this.addToMeal(recipe);
                return true;
            }),
            catchError(() => of(false)),
        );
    }

    public removePickerFavorite(favorite: FavoriteRecipe): Observable<boolean> {
        return this.removeFavorite(favorite).pipe(
            map(() => true),
            catchError(() => of(false)),
        );
    }

    public restorePickerFavorite(favorite: FavoriteRecipe): Observable<boolean> {
        return this.favoriteRecipeService.add(favorite.recipeId, favorite.name ?? undefined).pipe(
            tap(restored => {
                favorite.id = restored.id;
                this.syncRecipeFavoriteState(favorite.recipeId, true, restored.id);
                this.applyFavoriteCountDelta(1);
            }),
            map(() => true),
            catchError(() => of(false)),
        );
    }

    public hasActiveFilters(onlyMine: boolean, filters: RecipeFilters): boolean {
        return onlyMine || this.hasStructuredFilters(filters);
    }

    public hasSearch(search: string | null): boolean {
        return this.hasSearchValue(search);
    }

    private applyFavoriteCountDelta(delta: 1 | -1): void {
        this.favoriteTotalCount.update(count => Math.max(0, count + delta));
        if (this.isFavoritesLoadingMore() || this.favoriteOverviewReadId !== null) {
            this.loadFavorites().subscribe();
        }
    }

    private syncRecipeFavoriteState(recipeId: string, isFavorite: boolean, favoriteRecipeId: FavoriteRecipeId | null): void {
        this.recipeData.items.update(items =>
            items.map(recipe => (recipe.id === recipeId ? { ...recipe, isFavorite, favoriteRecipeId } : recipe)),
        );
        this.recentRecipes.update(recipes =>
            recipes.map(recipe => (recipe.id === recipeId ? { ...recipe, isFavorite, favoriteRecipeId } : recipe)),
        );
    }

    private removeRecipeFavorite(recipe: Recipe): Observable<void> {
        const favoriteId = recipe.favoriteRecipeId;
        const request$ =
            favoriteId !== null && favoriteId !== undefined && favoriteId.length > 0
                ? this.favoriteRecipeService.remove(favoriteId)
                : this.favoriteRecipeService.getLookupPage().pipe(
                      switchMap(favorites => {
                          const match = favorites.find(favorite => favorite.recipeId === recipe.id);
                          return match === undefined ? of(null) : this.favoriteRecipeService.remove(match.id);
                      }),
                  );

        return request$.pipe(
            tap(() => {
                this.syncRecipeFavoriteState(recipe.id, false, null);
            }),
            switchMap(() => this.loadFavorites()),
            catchError(() => {
                this.toastService.error(this.translateService.instant('RECIPE_LIST.FAVORITE_ERROR'));
                return of(void 0);
            }),
            finalize(() => {
                this.setFavoriteLoading(recipe.id, false);
            }),
        );
    }

    private setFavoriteLoading(recipeId: string, isLoading: boolean): void {
        this.favoriteLoadingIds.update(current => {
            const next = new Set(current);
            if (isLoading) {
                next.add(recipeId);
            } else {
                next.delete(recipeId);
            }

            return next;
        });
    }

    private hasSearchValue(value: string | null): boolean {
        return value !== null && value.trim().length > 0;
    }

    private hasStructuredFilters(filters: RecipeFilters): boolean {
        return (
            this.hasSearchValue(filters.category ?? null) ||
            this.hasOptionalValue(filters.maxTotalTime) ||
            this.hasOptionalValue(filters.caloriesFrom) ||
            this.hasOptionalValue(filters.caloriesTo) ||
            this.hasOptionalValue(filters.hasImage)
        );
    }

    private hasOptionalValue(value: number | boolean | null | undefined): boolean {
        return value !== null && value !== undefined;
    }
}

export type RecipeListFilterDialogData = {
    onlyMine: boolean;
    category: string | null;
    maxTotalTime: number | null;
    caloriesFrom: number | null;
    caloriesTo: number | null;
    hasImage: boolean | null;
};
