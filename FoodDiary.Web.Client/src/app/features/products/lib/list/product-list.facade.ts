import { computed, DestroyRef, effect, inject, Injectable, signal } from '@angular/core';
import { takeUntilDestroyed, toObservable } from '@angular/core/rxjs-interop';
import { form } from '@angular/forms/signals';
import { TranslateService } from '@ngx-translate/core';
import { FdUiDialogService } from 'fd-ui-kit/dialog/fd-ui-dialog.service';
import { FdUiToastService } from 'fd-ui-kit/toast/fd-ui-toast.service';
import type { Subscription } from 'rxjs';
import {
    catchError,
    debounceTime,
    distinctUntilChanged,
    EMPTY,
    filter,
    finalize,
    firstValueFrom,
    map,
    type Observable,
    of,
    skip,
    Subject,
    switchMap,
    take,
    takeUntil,
    tap,
} from 'rxjs';

import { BarcodeScannerComponent } from '../../../../components/shared/barcode-scanner/barcode-scanner';
import { APP_SEARCH_DEBOUNCE_MS } from '../../../../config/runtime-ui.tokens';
import { NavigationService } from '../../../../services/navigation.service';
import { resolveProductImageUrl } from '../../../../shared/lib/product-image.util';
import { RequestPagedData } from '../../../../shared/lib/request-paged-data';
import { RequestStateController } from '../../../../shared/lib/request-state';
import type { PageOf } from '../../../../shared/models/page-of.data';
import { type FavoriteProduct, type Product, ProductFilters, ProductType } from '../../../../shared/models/product.data';
import { resolvePaginationPage } from '../../../../shared/navigation/pagination-query.utils';
import { ViewportService } from '../../../../shared/platform/viewport.service';
import { QuickMealService } from '../../../meals/contracts/quick-meal';
import { FavoriteProductService } from '../../api/favorite-product.service';
import { OpenFoodFactsService } from '../../api/open-food-facts.service';
import { ProductService } from '../../api/product.service';
import { ProductDetailActionResult } from '../../components/detail/product-detail-lib/product-detail.types';
import {
    PRODUCT_LIST_FAVORITE_LIMIT,
    PRODUCT_LIST_OFF_SEARCH_LIMIT,
    PRODUCT_LIST_OFF_SEARCH_MIN_LENGTH,
    PRODUCT_LIST_PAGE_SIZE,
    PRODUCT_LIST_RECENT_LIMIT,
} from '../../components/list/product-list.config';
import type { ProductCardViewModel } from '../../components/list/product-list.types';
import { ProductListFiltersDialogComponent } from '../../components/list/product-list-filters-dialog/product-list-filters-dialog';
import type { ProductListFiltersDialogResult } from '../../components/list/product-list-filters-dialog/product-list-filters-dialog.types';
import {
    ProductFavoritesPickerComponent,
    type ProductFavoritesPickerData,
} from '../../dialogs/product-favorites-picker/product-favorites-picker';
import type { OpenFoodFactsProduct } from '../../models/open-food-facts.data';
import { buildFavoriteProductSnapshot, getProductListActiveFilterCount, resolveProductListFilterChanges } from './product-list.state';
import { normalizeProductListSearch, type ProductListQuery, productListQueryKey } from './product-list-query';
import { PRODUCT_LIST_QUERY_STATE } from './product-list-query-state';

@Injectable()
export class ProductListFacade {
    private readonly productService = inject(ProductService);
    public readonly navigationService = inject(NavigationService);
    public readonly fdDialogService = inject(FdUiDialogService);
    private readonly quickMealService = inject(QuickMealService);
    private readonly favoriteProductService = inject(FavoriteProductService);
    private readonly viewportService = inject(ViewportService);
    private readonly destroyRef = inject(DestroyRef);
    private readonly openFoodFactsService = inject(OpenFoodFactsService);
    private readonly searchDebounceMs = inject(APP_SEARCH_DEBOUNCE_MS);
    private readonly toastService = inject(FdUiToastService);
    private readonly translateService = inject(TranslateService);
    private isDeleteInProgress = false;
    private readonly queryState = inject(PRODUCT_LIST_QUERY_STATE, { optional: true });
    private activeRouteQuery: ProductListQuery | null = null;
    private readonly navigationError = signal<string | null>(null);
    private readonly deletingProduct = signal(false);
    private readonly cancelLoad = new Subject<void>();

    public readonly pageSize = PRODUCT_LIST_PAGE_SIZE;
    public readonly searchModel = signal<ProductSearchFormValues>({
        search: null,
        onlyMine: false,
    });
    public readonly searchForm = form(this.searchModel);
    public readonly productData = new RequestPagedData<Product>(this.deletingProduct);
    private readonly pageIndex = signal(0);
    public get currentPageIndex(): number {
        return this.pageIndex();
    }
    public set currentPageIndex(value: number) {
        this.pageIndex.set(value);
    }
    public readonly recentProducts = signal<Product[]>([]);
    public readonly favorites = signal<FavoriteProduct[]>([]);
    public readonly favoriteTotalCount = signal(0);
    public readonly isFavoritesOpen = signal(false);
    public readonly favoriteLoadingIds = signal<ReadonlySet<string>>(new Set<string>());
    public readonly isFavoritesLoadingMore = signal(false);
    public readonly errorKey = computed(() => this.navigationError() ?? this.productData.error());
    public readonly searchValue = computed(() => this.searchModel().search);
    public readonly onlyMineFilter = computed(() => this.searchModel().onlyMine);
    public readonly isMobileView = this.viewportService.isMobile;
    public readonly hasSearchValue = computed(() => (this.searchValue()?.trim().length ?? 0) > 0);
    public readonly showRecentSection = computed(
        () => this.pageIndex() === 0 && !this.hasSearchValue() && !this.hasActiveFilters() && this.recentProducts().length > 0,
    );
    public readonly recentProductItems = computed<ProductCardViewModel[]>(() => {
        if (!this.showRecentSection()) {
            return [];
        }

        return this.recentProducts().map(product => ({
            product,
            imageUrl: this.resolveImage(product),
        }));
    });
    public readonly allProductsSectionItems = computed(() => this.productData.items());
    public readonly allProductItems = computed<ProductCardViewModel[]>(() =>
        this.allProductsSectionItems().map(product => ({
            product,
            imageUrl: this.resolveImage(product),
        })),
    );
    public readonly selectedProductTypes = signal<ProductType[]>([]);
    public readonly caloriesFromFilter = signal<number | null>(null);
    public readonly caloriesToFilter = signal<number | null>(null);
    public readonly hasImageFilter = signal<boolean | null>(null);
    public readonly hasVisibleProducts = computed(() => this.showRecentSection() || this.allProductsSectionItems().length > 0);
    public readonly activeFilterCount = computed(() =>
        getProductListActiveFilterCount({
            onlyMine: this.onlyMineFilter(),
            productTypes: this.selectedProductTypes(),
            caloriesFrom: this.caloriesFromFilter(),
            caloriesTo: this.caloriesToFilter(),
            hasImage: this.hasImageFilter(),
        }),
    );
    public readonly hasActiveFilters = computed(() => this.activeFilterCount() > 0);
    public readonly isEmptyState = computed(
        () => this.productData.totalItems === 0 && !this.hasVisibleProducts() && !this.hasSearchValue() && !this.hasActiveFilters(),
    );
    public readonly allProductsSectionLabelKey = computed(() =>
        this.hasSearchValue() ? 'PRODUCT_LIST.SEARCH_RESULTS' : 'PRODUCT_LIST.ALL_PRODUCTS',
    );
    public readonly isMobileSearchVisible = computed(() => this.isMobileSearchOpen() || this.hasSearchValue());
    private readonly offRequest = new RequestStateController<OpenFoodFactsProduct[]>();
    private offRead: Subscription | undefined;
    public readonly offProducts = computed(() => this.offRequest.data() ?? []);
    public readonly offLoading = this.offRequest.isLoading;
    private readonly isMobileSearchOpen = signal(false);

    public clearSearch(): void {
        this.searchForm.search().value.set('');
    }

    public constructor() {
        effect(() => {
            if (!this.isMobileView()) {
                this.isMobileSearchOpen.set(false);
            }
        });

        if (this.queryState !== null) {
            this.applyRouteQuery(this.queryState.initial);
            this.queryState.changes
                .pipe(
                    filter(
                        query =>
                            this.activeRouteQuery === null || productListQueryKey(query) !== productListQueryKey(this.activeRouteQuery),
                    ),
                    tap(query => {
                        this.applyRouteQuery(query);
                    }),
                    switchMap(query => this.loadProducts(query.page, this.pageSize, query.search)),
                    takeUntilDestroyed(this.destroyRef),
                )
                .subscribe();
        }
        this.loadInitialOverview().subscribe();
        this.bindSearch();
    }

    public resolveImage(product: Product): string | undefined {
        return resolveProductImageUrl(product.imageUrl ?? undefined, product.productType ?? ProductType.Unknown);
    }

    public retryLoad(): void {
        this.loadProducts(this.currentPageIndex + 1, this.pageSize, this.searchValue()).subscribe();
    }

    public onPageChange(pageIndex: number): void {
        this.currentPageIndex = pageIndex;
        this.loadProducts(this.currentPageIndex + 1, this.pageSize, this.searchValue()).subscribe();
    }

    public onAddProductClick(): void {
        void this.navigationService.navigateToProductAddAsync();
    }

    public openBarcodeScanner(): void {
        this.fdDialogService
            .open<BarcodeScannerComponent, null, string | null>(BarcodeScannerComponent, {
                size: 'lg',
            })
            .afterClosed()
            .pipe(take(1))
            .subscribe(barcode => {
                if (barcode !== null && barcode !== undefined && barcode.length > 0) {
                    this.searchForm.search().value.set(barcode);
                }
            });
    }

    public toggleOnlyMine(): void {
        this.searchForm.onlyMine().value.set(!this.onlyMineFilter());
    }

    public toggleMobileSearch(): void {
        this.isMobileSearchOpen.update(value => !value);
    }

    public openFilters(): void {
        const currentOnlyMine = this.onlyMineFilter();
        const currentTypes = this.selectedProductTypes();
        const currentCaloriesFrom = this.caloriesFromFilter();
        const currentCaloriesTo = this.caloriesToFilter();
        const currentHasImage = this.hasImageFilter();

        this.fdDialogService
            .open<
                ProductListFiltersDialogComponent,
                {
                    onlyMine: boolean;
                    productTypes: ProductType[];
                    caloriesFrom: number | null;
                    caloriesTo: number | null;
                    hasImage: boolean | null;
                },
                ProductListFiltersDialogResult | null
            >(ProductListFiltersDialogComponent, {
                preset: 'form',
                data: {
                    onlyMine: currentOnlyMine,
                    productTypes: [...currentTypes],
                    caloriesFrom: currentCaloriesFrom,
                    caloriesTo: currentCaloriesTo,
                    hasImage: currentHasImage,
                },
            })
            .afterClosed()
            .pipe(
                switchMap(result => {
                    if (result === null || result === undefined) {
                        return EMPTY;
                    }

                    const changes = resolveProductListFilterChanges(
                        {
                            onlyMine: currentOnlyMine,
                            productTypes: currentTypes,
                            caloriesFrom: currentCaloriesFrom,
                            caloriesTo: currentCaloriesTo,
                            hasImage: currentHasImage,
                        },
                        result,
                    );

                    if (!changes.hasChanges) {
                        return EMPTY;
                    }

                    if (changes.typesChanged) {
                        this.selectedProductTypes.set(changes.productTypes);
                    }

                    if (changes.caloriesChanged) {
                        this.caloriesFromFilter.set(result.caloriesFrom);
                        this.caloriesToFilter.set(result.caloriesTo);
                    }

                    if (changes.imageChanged) {
                        this.hasImageFilter.set(result.hasImage);
                    }

                    if (changes.onlyMineChanged) {
                        this.searchForm.onlyMine().value.set(result.onlyMine);
                        return EMPTY;
                    }

                    return this.loadProducts(1, this.pageSize, this.searchValue());
                }),
                takeUntilDestroyed(this.destroyRef),
            )
            .subscribe();
    }

    public loadProducts(page: number, limit: number, search: string | null): Observable<void> {
        const query = this.currentQuery(page, search);
        if (
            this.queryState !== null &&
            (this.activeRouteQuery === null || productListQueryKey(query) !== productListQueryKey(this.activeRouteQuery))
        ) {
            void this.queryState.writeAsync(query).catch(() => {
                this.navigationError.set('ERRORS.LOAD_FAILED_TITLE');
            });
            return EMPTY;
        }
        if (page === 1 && (search?.trim().length ?? 0) === 0 && !this.hasActiveFilters()) {
            return this.loadInitialOverview();
        }
        this.cancelLoad.next();
        const requestId = this.productData.begin();
        this.navigationError.set(null);

        const filters = new ProductFilters({
            search,
            productTypes: this.selectedProductTypes(),
            caloriesFrom: this.caloriesFromFilter(),
            caloriesTo: this.caloriesToFilter(),
            hasImage: this.hasImageFilter(),
        });
        const includePublic = !this.onlyMineFilter();

        this.searchOpenFoodFacts(search);

        return this.productService.query(page, limit, filters, includePublic).pipe(
            takeUntil(this.cancelLoad),
            takeUntilDestroyed(this.destroyRef),
            tap(data => {
                if (!this.acceptPage(requestId, query, data)) {
                    return;
                }
                this.currentPageIndex = data.page - 1;
            }),
            map(() => void 0),
            catchError((_error: unknown) => {
                if (!this.productData.fail(requestId, 'ERRORS.LOAD_FAILED_TITLE')) {
                    return of(void 0);
                }
                this.recentProducts.set([]);

                return of(void 0);
            }),
        );
    }

    public loadInitialOverview(): Observable<void> {
        this.cancelLoad.next();
        const requestId = this.productData.begin();
        this.navigationError.set(null);
        const query = this.currentQuery(this.queryState === null ? 1 : this.currentPageIndex + 1, this.searchValue());

        this.searchOpenFoodFacts(this.searchValue());

        return this.productService
            .queryOverview({
                page: this.queryState === null ? 1 : this.currentPageIndex + 1,
                limit: this.pageSize,
                includePublic: !this.onlyMineFilter(),
                ...(this.queryState === null ? {} : { filters: this.currentFilters(this.searchValue()) }),
                recentLimit: PRODUCT_LIST_RECENT_LIMIT,
                favoriteLimit: PRODUCT_LIST_FAVORITE_LIMIT,
            })
            .pipe(
                takeUntil(this.cancelLoad),
                takeUntilDestroyed(this.destroyRef),
                tap(data => {
                    if (!this.isCurrentPageRequest(requestId, query)) {
                        return;
                    }
                    this.recentProducts.set(data.recentItems);
                    this.favorites.set(data.favoriteItems);
                    this.favoriteTotalCount.set(data.favoriteTotalCount);
                    if (!this.acceptPage(requestId, query, data.allProducts)) {
                        return;
                    }
                    this.currentPageIndex = data.allProducts.page - 1;
                }),
                map(() => void 0),
                catchError((_error: unknown) => {
                    if (!this.productData.fail(requestId, 'ERRORS.LOAD_FAILED_TITLE')) {
                        return of(void 0);
                    }
                    this.recentProducts.set([]);
                    this.favorites.set([]);
                    this.favoriteTotalCount.set(0);

                    return of(void 0);
                }),
            );
    }

    public onOffProductClick(offProduct: OpenFoodFactsProduct): void {
        void this.navigationService.navigateToProductAddAsync({
            state: {
                barcode: offProduct.barcode,
                offProduct,
            },
        });
    }

    public onAddToMeal(product: Product): void {
        this.quickMealService.addProduct(product);
    }

    public loadFavorites(): void {
        this.favoriteProductService
            .getPage(1, 1)
            .pipe(takeUntilDestroyed(this.destroyRef))
            .subscribe({
                next: result => {
                    this.favoriteTotalCount.set(result.totalItems);
                },
                error: () => this.toastService.error(this.translateService.instant('PRODUCT_FAVORITES.LOAD_ERROR')),
            });
    }

    public openFavorites(): void {
        this.fdDialogService.open<ProductFavoritesPickerComponent, ProductFavoritesPickerData, boolean>(ProductFavoritesPickerComponent, {
            size: 'md',
            data: {
                repeat: favorite =>
                    this.productService.getById(favorite.productId).pipe(
                        map(product => {
                            if (product === null) {
                                return false;
                            }
                            this.quickMealService.addProduct(product, favorite.preferredPortionAmount);
                            return true;
                        }),
                    ),
                remove: favorite =>
                    this.favoriteProductService.remove(favorite.id).pipe(
                        tap(() => {
                            this.favoriteTotalCount.update(count => Math.max(0, count - 1));
                            this.syncProductFavoriteState(favorite.productId, false, null);
                        }),
                        map(() => true),
                    ),
                restore: favorite =>
                    this.favoriteProductService.add(favorite.productId, favorite.name ?? undefined, favorite.preferredPortionAmount).pipe(
                        tap(restored => {
                            favorite.id = restored.id;
                            this.favoriteTotalCount.update(count => count + 1);
                            this.syncProductFavoriteState(favorite.productId, true, restored.id);
                        }),
                        map(() => true),
                    ),
            },
        });
    }

    public onProductFavoriteToggle(product: Product): void {
        if (this.favoriteLoadingIds().has(product.id)) {
            return;
        }

        this.setFavoriteLoading(product.id, true);

        if (product.isFavorite === true) {
            this.removeProductFavorite(product);
            return;
        }

        this.favoriteProductService
            .add(product.id, product.name, product.defaultPortionAmount)
            .pipe(
                takeUntilDestroyed(this.destroyRef),
                finalize(() => {
                    this.setFavoriteLoading(product.id, false);
                }),
            )
            .subscribe({
                next: favorite => {
                    this.syncProductFavoriteState(product.id, true, favorite.id);
                    this.favoriteTotalCount.update(count => count + 1);
                },
                error: () => this.toastService.error(this.translateService.instant('PRODUCT_FAVORITES.ADD_ERROR')),
            });
    }

    public toggleFavorites(): void {
        this.isFavoritesOpen.update(value => !value);
    }

    public openFavoriteProduct(favorite: FavoriteProduct, onProduct: (product: Product) => void): void {
        this.productService
            .getById(favorite.productId)
            .pipe(takeUntilDestroyed(this.destroyRef))
            .subscribe(product => {
                if (product !== null) {
                    onProduct(product);
                }
            });
    }

    public addFavoriteProductToMeal(favorite: FavoriteProduct): void {
        this.quickMealService.addProduct(buildFavoriteProductSnapshot(favorite), favorite.preferredPortionAmount);
    }

    public removeFavorite(favorite: FavoriteProduct): void {
        if (this.favoriteLoadingIds().has(favorite.productId)) {
            return;
        }

        this.setFavoriteLoading(favorite.productId, true);
        this.favoriteProductService
            .remove(favorite.id)
            .pipe(
                take(1),
                finalize(() => {
                    this.setFavoriteLoading(favorite.productId, false);
                }),
            )
            .subscribe({
                next: () => {
                    this.favorites.update(favorites => favorites.filter(item => item.id !== favorite.id));
                    this.favoriteTotalCount.update(count => Math.max(0, count - 1));
                    this.syncProductFavoriteState(favorite.productId, false, null);
                },
            });
    }

    public reloadCurrentPage(): void {
        this.loadProducts(this.currentPageIndex + 1, this.pageSize, this.searchValue()).subscribe();
    }

    public deleteProductAndReload(productId: string): Observable<void> {
        this.deletingProduct.set(true);
        return this.productService.deleteById(productId).pipe(
            switchMap(() => this.loadProducts(this.currentPageIndex + 1, this.pageSize, this.searchValue())),
            takeUntilDestroyed(this.destroyRef),
            finalize(() => {
                this.deletingProduct.set(false);
            }),
        );
    }

    public async handleProductDetailsAsync(product: Product): Promise<boolean> {
        const { ProductDetailComponent } = await import('../../components/detail/product-detail/product-detail');
        const result = await firstValueFrom(
            this.fdDialogService.open(ProductDetailComponent, { preset: 'detail', data: product }).afterClosed().pipe(take(1)),
        );

        if (!(result instanceof ProductDetailActionResult)) {
            return false;
        }

        if (result.action === 'FavoriteChanged') {
            this.loadFavorites();
            this.reloadCurrentPage();
            return false;
        }

        if (result.action === 'Edit' || result.action === 'Duplicate') {
            await this.navigationService.navigateToProductEditAsync(result.id);
            return false;
        }

        if (!product.isOwnedByCurrentUser || this.isDeleteInProgress) {
            return false;
        }

        this.isDeleteInProgress = true;
        try {
            await firstValueFrom(this.deleteProductAndReload(result.id));
            return true;
        } catch {
            this.deletingProduct.set(false);
            this.toastService.error(this.translateService.instant('PRODUCT_LIST.DELETE_ERROR'));
            return false;
        } finally {
            this.isDeleteInProgress = false;
        }
    }

    private bindSearch(): void {
        toObservable(this.searchValue)
            .pipe(
                skip(1),
                debounceTime(this.searchDebounceMs),
                distinctUntilChanged(),
                switchMap(value =>
                    this.routeModelMatches(value, this.onlyMineFilter()) ? EMPTY : this.loadProducts(1, this.pageSize, value),
                ),
                takeUntilDestroyed(this.destroyRef),
            )
            .subscribe();

        toObservable(this.onlyMineFilter)
            .pipe(
                skip(1),
                distinctUntilChanged(),
                switchMap(() =>
                    this.routeModelMatches(this.searchValue(), this.onlyMineFilter())
                        ? EMPTY
                        : this.loadProducts(1, this.pageSize, this.searchValue()),
                ),
                takeUntilDestroyed(this.destroyRef),
            )
            .subscribe();
    }

    private searchOpenFoodFacts(search: string | null): void {
        this.offRead?.unsubscribe();
        this.offRequest.reset();
        const trimmed = search?.trim();
        if (trimmed === undefined || trimmed.length < PRODUCT_LIST_OFF_SEARCH_MIN_LENGTH) {
            return;
        }
        const requestId = this.offRequest.begin();
        this.offRead = this.openFoodFactsService
            .search(trimmed, PRODUCT_LIST_OFF_SEARCH_LIMIT)
            .pipe(takeUntilDestroyed(this.destroyRef))
            .subscribe({
                next: products => this.offRequest.succeed(requestId, products),
                error: () => this.offRequest.fail(requestId, 'ERRORS.LOAD_FAILED_TITLE', { preserveData: false }),
            });
    }

    private currentFilters(search: string | null): ProductFilters {
        return new ProductFilters({
            search,
            productTypes: this.selectedProductTypes(),
            caloriesFrom: this.caloriesFromFilter(),
            caloriesTo: this.caloriesToFilter(),
            hasImage: this.hasImageFilter(),
        });
    }

    private currentQuery(page: number, search: string | null): ProductListQuery {
        return {
            page,
            search: normalizeProductListSearch(search),
            onlyMine: this.onlyMineFilter(),
            productTypes: this.selectedProductTypes(),
            caloriesFrom: this.caloriesFromFilter(),
            caloriesTo: this.caloriesToFilter(),
            hasImage: this.hasImageFilter(),
        };
    }

    private acceptPage(requestId: number, query: ProductListQuery, data: PageOf<Product>): boolean {
        if (!this.isCurrentPageRequest(requestId, query)) {
            return false;
        }
        const page = resolvePaginationPage(data.page, data.totalPages);
        if (page !== data.page) {
            if (this.queryState !== null) {
                this.replaceRouteQuery({ ...query, page });
            } else {
                this.loadProducts(page, this.pageSize, query.search).subscribe();
            }
            return false;
        }
        if (this.queryState !== null) {
            void this.queryState.normalizePageAsync?.(query).catch(() => {
                this.navigationError.set('ERRORS.LOAD_FAILED_TITLE');
            });
        }
        return this.productData.succeed(requestId, data);
    }

    private isCurrentPageRequest(requestId: number, query: ProductListQuery): boolean {
        return (
            this.productData.isCurrent(requestId) &&
            (this.queryState === null ||
                productListQueryKey(query) === productListQueryKey(this.currentQuery(this.currentPageIndex + 1, this.searchValue())))
        );
    }

    private replaceRouteQuery(query: ProductListQuery): void {
        void this.queryState?.writeAsync(query, { replaceUrl: true }).catch(() => {
            this.navigationError.set('ERRORS.LOAD_FAILED_TITLE');
        });
    }

    private applyRouteQuery(query: ProductListQuery): void {
        this.activeRouteQuery = query;
        this.searchModel.set({ search: query.search, onlyMine: query.onlyMine });
        this.selectedProductTypes.set(query.productTypes);
        this.caloriesFromFilter.set(query.caloriesFrom);
        this.caloriesToFilter.set(query.caloriesTo);
        this.hasImageFilter.set(query.hasImage);
        this.currentPageIndex = query.page - 1;
    }

    private routeModelMatches(search: string | null, onlyMine: boolean): boolean {
        return (
            this.activeRouteQuery !== null &&
            this.activeRouteQuery.search === normalizeProductListSearch(search) &&
            this.activeRouteQuery.onlyMine === onlyMine
        );
    }

    private syncProductFavoriteState(productId: string, isFavorite: boolean, favoriteProductId: string | null): void {
        this.productData.updateItems(items =>
            items.map(product => (product.id === productId ? { ...product, isFavorite, favoriteProductId } : product)),
        );
        this.recentProducts.update(products =>
            products.map(product => (product.id === productId ? { ...product, isFavorite, favoriteProductId } : product)),
        );
    }

    private removeProductFavorite(product: Product): void {
        const favoriteId = product.favoriteProductId;
        const request$ =
            favoriteId !== null && favoriteId !== undefined && favoriteId.length > 0
                ? this.favoriteProductService.remove(favoriteId)
                : this.favoriteProductService.getLookupPage().pipe(
                      switchMap(favorites => {
                          const match = favorites.find(favorite => favorite.productId === product.id);
                          return match === undefined ? of(null) : this.favoriteProductService.remove(match.id);
                      }),
                  );

        request$
            .pipe(
                takeUntilDestroyed(this.destroyRef),
                finalize(() => {
                    this.setFavoriteLoading(product.id, false);
                }),
            )
            .subscribe({
                next: () => {
                    this.syncProductFavoriteState(product.id, false, null);
                    this.favoriteTotalCount.update(count => Math.max(0, count - 1));
                },
                error: () => this.toastService.error(this.translateService.instant('PRODUCT_FAVORITES.REMOVE_ERROR')),
            });
    }

    private setFavoriteLoading(productId: string, isLoading: boolean): void {
        this.favoriteLoadingIds.update(current => {
            const next = new Set(current);
            if (isLoading) {
                next.add(productId);
            } else {
                next.delete(productId);
            }

            return next;
        });
    }
}

type ProductSearchFormValues = {
    search: string | null;
    onlyMine: boolean;
};
