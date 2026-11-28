import { computed, DestroyRef, effect, inject, Injectable, Injector, signal } from '@angular/core';
import { rxResource, takeUntilDestroyed, toObservable } from '@angular/core/rxjs-interop';
import { form } from '@angular/forms/signals';
import { TranslateService } from '@ngx-translate/core';
import { FdUiDialogService } from 'fd-ui-kit/dialog/fd-ui-dialog.service';
import type { FdUiDialogRef } from 'fd-ui-kit/dialog/fd-ui-dialog-ref';
import { FdUiToastService } from 'fd-ui-kit/toast/fd-ui-toast.service';
import {
    catchError,
    debounceTime,
    defaultIfEmpty,
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
    type Subscription,
    switchMap,
    take,
    takeUntil,
    tap,
} from 'rxjs';

import { BarcodeScannerComponent } from '../../../../components/shared/barcode-scanner/barcode-scanner';
import { APP_SEARCH_DEBOUNCE_MS } from '../../../../config/runtime-ui.tokens';
import { NavigationService } from '../../../../services/navigation.service';
import { developmentRequestContext } from '../../../../shared/development/development-request-context';
import { injectCurrentLanguage } from '../../../../shared/i18n/inject-current-language';
import { resolveProductImageUrl } from '../../../../shared/lib/product-image.util';
import { RequestPagedData } from '../../../../shared/lib/request-paged-data';
import type { PageOf } from '../../../../shared/models/page-of.data';
import { type FavoriteProduct, type Product, ProductFilters, ProductType } from '../../../../shared/models/product.data';
import type { FavoriteProductId, ProductId } from '../../../../shared/models/semantics/entity-id';
import { entityId } from '../../../../shared/models/semantics/entity-id';
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
import { getProductListActiveFilterCount, resolveProductListFilterChanges } from './product-list.state';
import { productFilterChips, removeProductFilter, resetProductFilters } from './product-list-filter-chips';
import { normalizeProductListSearch, type ProductListQuery, productListQueryKey } from './product-list-query';
import { PRODUCT_LIST_QUERY_STATE } from './product-list-query-state';

type ProductListRead = {
    id: number;
    query: ProductListQuery;
    filters: ProductFilters;
    overview: boolean;
};

@Injectable()
export class ProductListFacade {
    private readonly productService = inject(ProductService);
    public readonly navigationService = inject(NavigationService);
    public readonly fdDialogService = inject(FdUiDialogService);
    private readonly quickMealService = inject(QuickMealService);
    private readonly favoriteProductService = inject(FavoriteProductService);
    private readonly viewportService = inject(ViewportService);
    private readonly destroyRef = inject(DestroyRef);
    private readonly injector = inject(Injector);
    private readonly openFoodFactsService = inject(OpenFoodFactsService);
    private readonly searchDebounceMs = inject(APP_SEARCH_DEBOUNCE_MS);
    private readonly toastService = inject(FdUiToastService);
    private readonly translateService = inject(TranslateService);
    private isDeleteInProgress = false;
    private readonly queryState = inject(PRODUCT_LIST_QUERY_STATE, { optional: true });
    private activeRouteQuery: ProductListQuery | null = null;
    private readonly navigationError = signal<string | null>(null);
    private readonly deletingProduct = signal(false);
    private readonly cancelCommandRead = new Subject<void>();
    private favoriteCountRead: Subscription | null = null;
    private favoriteCountRevision = 0;
    private favoriteOverviewReadId: number | null = null;

    public readonly pageSize = PRODUCT_LIST_PAGE_SIZE;
    public readonly searchModel = signal<ProductSearchFormValues>({
        search: null,
        onlyMine: false,
    });
    public readonly searchForm = form(this.searchModel);
    public readonly productData = new RequestPagedData<Product>(this.deletingProduct);
    private readonly viewRead = signal<ProductListRead | undefined>(undefined);
    private readonly viewResource = rxResource({
        params: this.viewRead,
        stream: ({ params }) => this.readProducts(params).pipe(defaultIfEmpty(undefined)),
    });
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
    public readonly favoriteLoadingIds = signal<ReadonlySet<string>>(new Set<string>());
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
    private readonly currentLanguage = injectCurrentLanguage();
    public readonly activeFilterChips = computed(() =>
        productFilterChips(this.currentQuery(1, this.searchValue()), key => this.translateService.instant(key), this.currentLanguage()),
    );
    public readonly isEmptyState = computed(
        () => this.productData.totalItems === 0 && !this.hasVisibleProducts() && !this.hasSearchValue() && !this.hasActiveFilters(),
    );
    public readonly allProductsSectionLabelKey = computed(() =>
        this.hasSearchValue() ? 'PRODUCT_LIST.SEARCH_RESULTS' : 'PRODUCT_LIST.ALL_PRODUCTS',
    );
    public readonly isMobileSearchVisible = computed(() => this.isMobileSearchOpen() || this.hasSearchValue());
    private readonly offQuery = signal<string | undefined>(undefined);
    private readonly offRequest = rxResource({
        params: this.offQuery,
        stream: ({ params }) => this.openFoodFactsService.search(params, PRODUCT_LIST_OFF_SEARCH_LIMIT),
        defaultValue: [],
    });
    public readonly offProducts = computed(() => (this.offRequest.hasValue() ? this.offRequest.value() : []));
    public readonly offLoading = this.offRequest.isLoading;
    public readonly offError = computed(() => this.offRequest.error() !== undefined);
    private readonly isMobileSearchOpen = signal(false);

    public clearSearch(): void {
        this.searchForm.search().value.set('');
    }

    public removeFilter(id: string): void {
        this.applyActiveFilterQuery(removeProductFilter(this.currentQuery(1, this.searchValue()), id));
    }

    public resetFilters(): void {
        this.applyActiveFilterQuery(resetProductFilters(this.currentQuery(1, this.searchValue())));
    }

    private applyActiveFilterQuery(query: ProductListQuery): void {
        void this.injector
            .get(PRODUCT_LIST_QUERY_STATE)
            .writeAsync(query)
            .catch(() => {
                this.navigationError.set('ERRORS.LOAD_FAILED_TITLE');
            });
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
                    takeUntilDestroyed(this.destroyRef),
                )
                .subscribe(query => {
                    this.applyRouteQuery(query);
                    this.requestProducts(query.page, query.search);
                });
        }
        this.requestOverview();
        this.translateService.onLangChange.pipe(takeUntilDestroyed(this.destroyRef)).subscribe(() => {
            this.requestOverview();
        });
        this.bindSearch();
    }

    public resolveImage(product: Product): string | undefined {
        return resolveProductImageUrl(product.imageUrl ?? undefined, product.productType ?? ProductType.Unknown);
    }

    public retryLoad(): void {
        this.requestProducts(this.currentPageIndex + 1, this.searchValue());
    }

    public retryOffSearch(): void {
        this.offRequest.reload();
    }

    public onPageChange(pageIndex: number): void {
        this.currentPageIndex = pageIndex;
        this.retryLoad();
    }

    public onAddProductClick(): void {
        void this.navigationService.navigateToProductAddAsync();
    }

    public openBarcodeScanner(): void {
        const dialog = this.fdDialogService.open<BarcodeScannerComponent, null, string | null>(BarcodeScannerComponent, { size: 'lg' });
        this.afterOwnedDialogClosed(dialog)
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

        const dialog = this.fdDialogService.open<
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
        });
        this.afterOwnedDialogClosed(dialog).subscribe(result => {
            if (result === null || result === undefined) {
                return;
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
                return;
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
                return;
            }

            this.requestProducts(1, this.searchValue());
        });
    }

    private requestProducts(page: number, search: string | null): void {
        const request = this.prepareProductsRead(page, search);
        if (request !== null) {
            this.viewRead.set(request);
        }
    }

    private prepareProductsRead(page: number, search: string | null): ProductListRead | null {
        const query = this.currentQuery(page, search);
        if (
            this.queryState !== null &&
            (this.activeRouteQuery === null || productListQueryKey(query) !== productListQueryKey(this.activeRouteQuery))
        ) {
            void this.queryState.writeAsync(query).catch(() => {
                this.navigationError.set('ERRORS.LOAD_FAILED_TITLE');
            });
            return null;
        }
        if (page === 1 && (search?.trim().length ?? 0) === 0 && !this.hasActiveFilters()) {
            return this.prepareOverviewRead();
        }
        return this.beginRead(query, search, false);
    }

    private requestOverview(): void {
        this.viewRead.set(this.prepareOverviewRead());
    }

    private prepareOverviewRead(): ProductListRead {
        const search = this.searchValue();
        const page = this.queryState === null ? 1 : this.currentPageIndex + 1;
        return this.beginRead(this.currentQuery(page, search), search, true);
    }

    private beginRead(query: ProductListQuery, search: string | null, overview: boolean): ProductListRead {
        this.viewResource.set(undefined);
        this.viewRead.set(undefined);
        this.cancelCommandRead.next();
        const id = this.productData.begin();
        this.navigationError.set(null);
        this.searchOpenFoodFacts(search);
        return { id, query, filters: this.currentFilters(search), overview };
    }

    private readProducts(request: ProductListRead): Observable<void> {
        const { id, query, filters, overview } = request;
        const favoriteCountRevision = this.favoriteCountRevision;
        if (overview) {
            this.favoriteOverviewReadId = id;
        }
        const page = overview
            ? this.readOverview(request, favoriteCountRevision)
            : this.productService.query(query.page, this.pageSize, filters, {
                  includePublic: !query.onlyMine,
                  context: developmentRequestContext(this.productData.bindRequest(id)),
              });
        return page.pipe(
            tap(data => {
                if (this.acceptPage(id, query, data)) {
                    this.currentPageIndex = data.page - 1;
                }
            }),
            map(() => void 0),
            catchError((_error: unknown) => {
                if (this.isCurrentPageRequest(id, query)) {
                    this.productData.fail(id, 'ERRORS.LOAD_FAILED_TITLE');
                    this.currentPageIndex = query.page - 1;
                    this.recentProducts.set([]);
                    if (overview) {
                        this.favorites.set([]);
                        if (favoriteCountRevision === this.favoriteCountRevision) {
                            this.favoriteTotalCount.set(0);
                        }
                    }
                }
                return of(void 0);
            }),
            finalize(() => {
                if (this.favoriteOverviewReadId === id) {
                    this.favoriteOverviewReadId = null;
                }
            }),
        );
    }

    private readOverview({ id, query, filters }: ProductListRead, favoriteCountRevision: number): Observable<PageOf<Product>> {
        return this.productService
            .queryOverview(
                {
                    page: query.page,
                    limit: this.pageSize,
                    includePublic: !query.onlyMine,
                    ...(this.queryState === null ? {} : { filters }),
                    recentLimit: PRODUCT_LIST_RECENT_LIMIT,
                    favoriteLimit: PRODUCT_LIST_FAVORITE_LIMIT,
                },
                developmentRequestContext(this.productData.bindRequest(id)),
            )
            .pipe(
                tap(data => {
                    if (!this.isCurrentPageRequest(id, query)) {
                        return;
                    }
                    this.recentProducts.set(data.recentItems);
                    this.favorites.set(data.favoriteItems);
                    if (favoriteCountRevision === this.favoriteCountRevision) {
                        this.favoriteCountRead?.unsubscribe();
                        this.favoriteTotalCount.set(data.favoriteTotalCount);
                    }
                }),
                map(data => data.allProducts),
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
        this.favoriteCountRevision++;
        this.favoriteCountRead?.unsubscribe();
        this.favoriteCountRead = this.favoriteProductService
            .getPage(1, 1)
            .pipe(take(1), takeUntilDestroyed(this.destroyRef))
            .subscribe({
                next: result => {
                    this.favoriteTotalCount.set(result.totalItems);
                },
                error: () => this.toastService.error(this.translateService.instant('PRODUCT_FAVORITES.LOAD_ERROR')),
            });
    }

    public openFavorites(): void {
        const dialog = this.fdDialogService.open<ProductFavoritesPickerComponent, ProductFavoritesPickerData, boolean>(
            ProductFavoritesPickerComponent,
            {
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
                                this.syncProductFavoriteState(favorite.productId, false, null);
                                this.applyFavoriteCountDelta(-1);
                            }),
                            map(() => true),
                        ),
                    restore: favorite =>
                        this.favoriteProductService
                            .add(favorite.productId, favorite.name ?? undefined, favorite.preferredPortionAmount)
                            .pipe(
                                tap(restored => {
                                    favorite.id = restored.id;
                                    this.syncProductFavoriteState(favorite.productId, true, restored.id);
                                    this.applyFavoriteCountDelta(1);
                                }),
                                map(() => true),
                            ),
                },
            },
        );
        const unregisterClose = this.destroyRef.onDestroy(() => {
            dialog.close();
        });
        dialog.afterClosed().pipe(takeUntilDestroyed(this.destroyRef), finalize(unregisterClose)).subscribe();
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
                    this.applyFavoriteCountDelta(1);
                },
                error: () => this.toastService.error(this.translateService.instant('PRODUCT_FAVORITES.ADD_ERROR')),
            });
    }

    public reloadCurrentPage(): void {
        this.retryLoad();
    }

    public deleteProductAndReload(productId: ProductId): Observable<void> {
        this.deletingProduct.set(true);
        return this.productService.deleteById(productId).pipe(
            switchMap(() => {
                const request = this.prepareProductsRead(this.currentPageIndex + 1, this.searchValue());
                return request === null ? EMPTY : this.readProducts(request).pipe(takeUntil(this.cancelCommandRead));
            }),
            takeUntilDestroyed(this.destroyRef),
            finalize(() => {
                this.deletingProduct.set(false);
            }),
        );
    }

    public async handleProductDetailsAsync(product: Product): Promise<boolean> {
        const result = await this.openProductDetailsAsync(product);

        if (this.destroyRef.destroyed || !(result instanceof ProductDetailActionResult)) {
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
        return this.deleteFromDetailsAsync(result.id);
    }

    private async deleteFromDetailsAsync(id: string): Promise<boolean> {
        this.isDeleteInProgress = true;
        try {
            const result = await firstValueFrom(this.deleteProductAndReload(entityId<'product'>(id)), { defaultValue: false });
            return result !== false && !this.destroyRef.destroyed;
        } catch {
            this.deletingProduct.set(false);
            if (!this.destroyRef.destroyed) {
                this.toastService.error(this.translateService.instant('PRODUCT_LIST.DELETE_ERROR'));
            }
            return false;
        } finally {
            this.isDeleteInProgress = false;
        }
    }

    private async openProductDetailsAsync(product: Product): Promise<unknown> {
        const { ProductDetailComponent } = await import('../../components/detail/product-detail/product-detail');
        if (this.destroyRef.destroyed) {
            return null;
        }
        const dialog = this.fdDialogService.open(ProductDetailComponent, { preset: 'detail', data: product });
        return firstValueFrom(this.afterOwnedDialogClosed(dialog), { defaultValue: null });
    }

    private afterOwnedDialogClosed<Result>(
        dialog: Pick<FdUiDialogRef<unknown, Result>, 'afterClosed' | 'close'>,
    ): Observable<Result | undefined> {
        return dialog.afterClosed().pipe(
            takeUntilDestroyed(this.destroyRef),
            finalize(() => {
                if (this.destroyRef.destroyed) {
                    dialog.close();
                }
            }),
        );
    }

    private bindSearch(): void {
        toObservable(this.searchValue)
            .pipe(skip(1), debounceTime(this.searchDebounceMs), distinctUntilChanged(), takeUntilDestroyed(this.destroyRef))
            .subscribe(value => {
                if (!this.routeModelMatches(value, this.onlyMineFilter())) {
                    this.requestProducts(1, value);
                }
            });

        toObservable(this.onlyMineFilter)
            .pipe(distinctUntilChanged(), skip(1), takeUntilDestroyed(this.destroyRef))
            .subscribe(onlyMine => {
                const search = this.searchValue();
                if (!this.routeModelMatches(search, onlyMine)) {
                    this.requestProducts(1, search);
                }
            });
    }

    private searchOpenFoodFacts(search: string | null): void {
        const trimmed = search?.trim();
        this.offQuery.set(trimmed === undefined || trimmed.length < PRODUCT_LIST_OFF_SEARCH_MIN_LENGTH ? undefined : trimmed);
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
                this.requestProducts(page, query.search);
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
        const appliedQuery = this.activeRouteQuery;
        return (
            this.productData.isCurrent(requestId) &&
            (this.queryState === null ||
                (productListQueryKey(query) === productListQueryKey(this.currentQuery(this.currentPageIndex + 1, this.searchValue())) &&
                    // Route input binding can trail the URL commit.
                    (appliedQuery === null || this.queryState.matchesCommittedQuery?.(appliedQuery) !== false)))
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

    private syncProductFavoriteState(productId: string, isFavorite: boolean, favoriteProductId: FavoriteProductId | null): void {
        this.productData.updateItems(items =>
            items.map(product => (product.id === productId ? { ...product, isFavorite, favoriteProductId } : product)),
        );
        this.recentProducts.update(products =>
            products.map(product => (product.id === productId ? { ...product, isFavorite, favoriteProductId } : product)),
        );
    }

    private applyFavoriteCountDelta(delta: 1 | -1): void {
        this.favoriteTotalCount.update(count => Math.max(0, count + delta));
        if (this.favoriteCountRead?.closed === false || this.favoriteOverviewReadId !== null) {
            this.loadFavorites();
        }
    }

    private removeProductFavorite(product: Product): void {
        const favoriteId = product.favoriteProductId;
        const favoriteId$ =
            favoriteId !== null && favoriteId !== undefined && favoriteId.length > 0
                ? of(favoriteId)
                : this.favoriteProductService
                      .getLookupPage()
                      .pipe(map(favorites => favorites.find(favorite => favorite.productId === product.id)?.id));

        favoriteId$
            .pipe(
                switchMap(id => (id === undefined ? of(false) : this.favoriteProductService.remove(id).pipe(map(() => true)))),
                takeUntilDestroyed(this.destroyRef),
                finalize(() => {
                    this.setFavoriteLoading(product.id, false);
                }),
            )
            .subscribe({
                next: removed => {
                    this.syncProductFavoriteState(product.id, false, null);
                    if (removed) {
                        this.applyFavoriteCountDelta(-1);
                    }
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
