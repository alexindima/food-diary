import { computed, DestroyRef, effect, inject, Service, signal } from '@angular/core';
import { takeUntilDestroyed, toObservable } from '@angular/core/rxjs-interop';
import { form } from '@angular/forms/signals';
import { FdUiDialogService } from 'fd-ui-kit/dialog/fd-ui-dialog.service';
import {
    catchError,
    debounceTime,
    distinctUntilChanged,
    EMPTY,
    finalize,
    map,
    type Observable,
    of,
    skip,
    switchMap,
    take,
    tap,
} from 'rxjs';

import { BarcodeScannerComponent } from '../../../../components/shared/barcode-scanner/barcode-scanner';
import { APP_SEARCH_DEBOUNCE_MS } from '../../../../config/runtime-ui.tokens';
import { NavigationService } from '../../../../services/navigation.service';
import { PagedData } from '../../../../shared/lib/paged-data.data';
import { ViewportService } from '../../../../shared/platform/viewport.service';
import { QuickMealService } from '../../../meals/lib/quick/quick-meal.service';
import { FavoriteProductService } from '../../api/favorite-product.service';
import { OpenFoodFactsService } from '../../api/open-food-facts.service';
import { ProductService } from '../../api/product.service';
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
import type { OpenFoodFactsProduct } from '../../models/open-food-facts.data';
import {
    type FavoriteProduct,
    MeasurementUnit,
    type Product,
    ProductFilters,
    ProductType,
    ProductVisibility,
} from '../../models/product.data';
import { resolveProductImageUrl } from '../product-image.util';

const FAVORITE_GRAM_BASE_AMOUNT = 100;

@Service()
export class ProductListFacade {
    private readonly productService = inject(ProductService);
    public readonly navigationService = inject(NavigationService);
    public readonly fdDialogService = inject(FdUiDialogService);
    private readonly quickConsumptionService = inject(QuickMealService);
    private readonly favoriteProductService = inject(FavoriteProductService);
    private readonly viewportService = inject(ViewportService);
    private readonly destroyRef = inject(DestroyRef);
    private readonly openFoodFactsService = inject(OpenFoodFactsService);
    private readonly searchDebounceMs = inject(APP_SEARCH_DEBOUNCE_MS);

    public readonly pageSize = PRODUCT_LIST_PAGE_SIZE;
    public readonly searchModel = signal<ProductSearchFormValues>({
        search: null,
        onlyMine: false,
    });
    public readonly searchForm = form(this.searchModel);
    public readonly productData = new PagedData<Product>();
    public currentPageIndex = 0;
    public readonly recentProducts = signal<Product[]>([]);
    public readonly favorites = signal<FavoriteProduct[]>([]);
    public readonly favoriteTotalCount = signal(0);
    public readonly isFavoritesOpen = signal(false);
    public readonly favoriteLoadingIds = signal<ReadonlySet<string>>(new Set<string>());
    public readonly isFavoritesLoadingMore = signal(false);
    public readonly errorKey = signal<string | null>(null);
    public readonly searchValue = computed(() => this.searchModel().search);
    public readonly onlyMineFilter = computed(() => this.searchModel().onlyMine);
    public readonly isMobileView = this.viewportService.isMobile;
    public readonly hasSearchValue = computed(() => (this.searchValue()?.trim().length ?? 0) > 0);
    public readonly showRecentSection = computed(() => !this.hasSearchValue() && this.recentProducts().length > 0);
    public readonly recentProductItems = computed<ProductCardViewModel[]>(() => {
        if (!this.showRecentSection()) {
            return [];
        }

        return this.recentProducts().map(product => ({
            product,
            imageUrl: this.resolveImage(product),
        }));
    });
    public readonly allProductsSectionItems = computed(() => {
        const products = this.productData.items();
        if (products.length === 0) {
            return [];
        }

        if (!this.showRecentSection()) {
            return products;
        }

        const recentIds = new Set(this.recentProducts().map(product => product.id));
        return products.filter(product => !recentIds.has(product.id));
    });
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
    public readonly activeFilterCount = computed(
        () =>
            (this.onlyMineFilter() ? 1 : 0) +
            this.selectedProductTypes().length +
            (this.caloriesFromFilter() !== null || this.caloriesToFilter() !== null ? 1 : 0) +
            (this.hasImageFilter() !== null ? 1 : 0),
    );
    public readonly hasActiveFilters = computed(() => this.activeFilterCount() > 0);
    public readonly isEmptyState = computed(() => !this.hasVisibleProducts() && !this.hasSearchValue() && !this.hasActiveFilters());
    public readonly allProductsSectionLabelKey = computed(() =>
        this.hasSearchValue() ? 'PRODUCT_LIST.SEARCH_RESULTS' : 'PRODUCT_LIST.ALL_PRODUCTS',
    );
    public readonly isMobileSearchVisible = computed(() => this.isMobileSearchOpen() || this.hasSearchValue());
    public readonly offProducts = signal<OpenFoodFactsProduct[]>([]);
    public readonly offLoading = signal(false);
    private readonly isMobileSearchOpen = signal(false);
    private offSearchRequestId = 0;

    public clearSearch(): void {
        this.searchForm.search().value.set('');
    }

    public constructor() {
        effect(() => {
            if (!this.isMobileView()) {
                this.isMobileSearchOpen.set(false);
            }
        });

        this.loadInitialOverview().subscribe();
        this.bindSearch();
    }

    public resolveImage(product: Product): string | undefined {
        return resolveProductImageUrl(product.imageUrl ?? undefined, product.productType ?? ProductType.Unknown);
    }

    public retryLoad(): void {
        this.loadInitialOverview().subscribe();
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

                    const changes = this.resolveFilterDialogChanges(
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
        this.productData.setLoading(true);
        this.offProducts.set([]);
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
            tap(data => {
                this.productData.setData(data);
                this.recentProducts.set([]);
                this.currentPageIndex = data.page - 1;
                this.errorKey.set(null);
            }),
            map(() => void 0),
            catchError((_error: unknown) => {
                this.productData.clearData();
                this.recentProducts.set([]);
                this.errorKey.set('ERRORS.LOAD_FAILED_TITLE');
                return of(void 0);
            }),
            finalize(() => {
                this.productData.setLoading(false);
            }),
        );
    }

    public loadInitialOverview(): Observable<void> {
        this.productData.setLoading(true);
        this.offProducts.set([]);
        this.searchOpenFoodFacts(this.searchValue());

        return this.productService
            .queryOverview({
                page: 1,
                limit: this.pageSize,
                includePublic: true,
                recentLimit: PRODUCT_LIST_RECENT_LIMIT,
                favoriteLimit: PRODUCT_LIST_FAVORITE_LIMIT,
            })
            .pipe(
                tap(data => {
                    this.productData.setData(data.allProducts);
                    this.recentProducts.set(data.recentItems);
                    this.favorites.set(data.favoriteItems);
                    this.favoriteTotalCount.set(data.favoriteTotalCount);
                    this.currentPageIndex = data.allProducts.page - 1;
                    this.errorKey.set(null);
                }),
                map(() => void 0),
                catchError((_error: unknown) => {
                    this.productData.clearData();
                    this.recentProducts.set([]);
                    this.favorites.set([]);
                    this.favoriteTotalCount.set(0);
                    this.errorKey.set('ERRORS.LOAD_FAILED_TITLE');
                    return of(void 0);
                }),
                finalize(() => {
                    this.productData.setLoading(false);
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
        this.quickConsumptionService.addProduct(product);
    }

    public loadFavorites(): void {
        this.isFavoritesLoadingMore.set(true);
        this.favoriteProductService
            .getAll()
            .pipe(
                takeUntilDestroyed(this.destroyRef),
                finalize(() => {
                    this.isFavoritesLoadingMore.set(false);
                }),
            )
            .subscribe(favorites => {
                this.favorites.set(favorites);
                this.favoriteTotalCount.set(favorites.length);
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
            .subscribe(favorite => {
                this.syncProductFavoriteState(product.id, true, favorite.id);
                this.loadFavorites();
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
        this.quickConsumptionService.addProduct(this.toFavoriteProductSnapshot(favorite), favorite.defaultPortionAmount);
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
        this.productData.setLoading(true);
        return this.productService
            .deleteById(productId)
            .pipe(switchMap(() => this.loadProducts(this.currentPageIndex + 1, this.pageSize, this.searchValue())));
    }

    private bindSearch(): void {
        toObservable(this.searchValue)
            .pipe(
                skip(1),
                debounceTime(this.searchDebounceMs),
                distinctUntilChanged(),
                switchMap(value => this.loadProducts(1, this.pageSize, value)),
                takeUntilDestroyed(this.destroyRef),
            )
            .subscribe();

        toObservable(this.onlyMineFilter)
            .pipe(
                skip(1),
                distinctUntilChanged(),
                switchMap(() => this.loadProducts(1, this.pageSize, this.searchValue())),
                takeUntilDestroyed(this.destroyRef),
            )
            .subscribe();
    }

    private searchOpenFoodFacts(search: string | null): void {
        const trimmed = search?.trim();
        if (trimmed === undefined || trimmed.length < PRODUCT_LIST_OFF_SEARCH_MIN_LENGTH) {
            this.offSearchRequestId++;
            this.offProducts.set([]);
            this.offLoading.set(false);
            return;
        }

        const requestId = ++this.offSearchRequestId;
        this.offLoading.set(true);
        this.openFoodFactsService
            .search(trimmed, PRODUCT_LIST_OFF_SEARCH_LIMIT)
            .pipe(
                catchError(() => of<OpenFoodFactsProduct[]>([])),
                takeUntilDestroyed(this.destroyRef),
                finalize(() => {
                    if (requestId === this.offSearchRequestId) {
                        this.offLoading.set(false);
                    }
                }),
            )
            .subscribe(products => {
                if (requestId === this.offSearchRequestId) {
                    this.offProducts.set(products);
                }
            });
    }

    private normalizeProductTypes(productTypes: ProductType[]): ProductType[] {
        return [...new Set(productTypes)];
    }

    private areProductTypesEqual(left: ProductType[], right: ProductType[]): boolean {
        if (left.length !== right.length) {
            return false;
        }

        const leftSet = new Set(left);
        return right.every(type => leftSet.has(type));
    }

    private resolveFilterDialogChanges(current: ProductListFilterState, result: ProductListFiltersDialogResult): ProductListFilterChanges {
        const productTypes = this.normalizeProductTypes(result.productTypes);
        const onlyMineChanged = current.onlyMine !== result.onlyMine;
        const typesChanged = !this.areProductTypesEqual(current.productTypes, productTypes);
        const caloriesChanged = current.caloriesFrom !== result.caloriesFrom || current.caloriesTo !== result.caloriesTo;
        const imageChanged = current.hasImage !== result.hasImage;

        return {
            productTypes,
            onlyMineChanged,
            typesChanged,
            caloriesChanged,
            imageChanged,
            hasChanges: onlyMineChanged || typesChanged || caloriesChanged || imageChanged,
        };
    }

    private syncProductFavoriteState(productId: string, isFavorite: boolean, favoriteProductId: string | null): void {
        this.productData.items.update(items =>
            items.map(product => (product.id === productId ? { ...product, isFavorite, favoriteProductId } : product)),
        );
        this.recentProducts.update(products =>
            products.map(product => (product.id === productId ? { ...product, isFavorite, favoriteProductId } : product)),
        );
    }

    private toFavoriteProductSnapshot(favorite: FavoriteProduct): Product {
        return {
            id: favorite.productId,
            name: this.resolveFavoriteName(favorite),
            barcode: favorite.barcode ?? null,
            brand: favorite.brand ?? null,
            productType: ProductType.Unknown,
            category: null,
            description: null,
            comment: favorite.comment ?? null,
            imageUrl: favorite.imageUrl ?? null,
            imageAssetId: null,
            baseUnit: this.normalizeMeasurementUnit(favorite.baseUnit),
            baseAmount: this.resolveFavoriteBaseAmount(favorite.baseUnit),
            defaultPortionAmount: favorite.defaultPortionAmount,
            caloriesPerBase: favorite.caloriesPerBase,
            proteinsPerBase: favorite.proteinsPerBase,
            fatsPerBase: favorite.fatsPerBase,
            carbsPerBase: favorite.carbsPerBase,
            fiberPerBase: favorite.fiberPerBase,
            alcoholPerBase: favorite.alcoholPerBase,
            usageCount: 0,
            visibility: ProductVisibility.Private,
            createdAt: new Date(favorite.createdAtUtc),
            isOwnedByCurrentUser: favorite.isOwnedByCurrentUser,
            qualityScore: favorite.qualityScore,
            qualityGrade: favorite.qualityGrade,
            isFavorite: true,
            favoriteProductId: favorite.id,
        };
    }

    private resolveFavoriteName(favorite: FavoriteProduct): string {
        const name = favorite.name?.trim();
        return name !== undefined && name.length > 0 ? name : favorite.productName;
    }

    private normalizeMeasurementUnit(value: string): MeasurementUnit {
        if (value === 'ML') {
            return MeasurementUnit.ML;
        }

        if (value === 'PCS') {
            return MeasurementUnit.PCS;
        }

        return MeasurementUnit.G;
    }

    private resolveFavoriteBaseAmount(baseUnit: string): number {
        return this.normalizeMeasurementUnit(baseUnit) === MeasurementUnit.PCS ? 1 : FAVORITE_GRAM_BASE_AMOUNT;
    }

    private removeProductFavorite(product: Product): void {
        const favoriteId = product.favoriteProductId;
        const request$ =
            favoriteId !== null && favoriteId !== undefined && favoriteId.length > 0
                ? this.favoriteProductService.remove(favoriteId)
                : this.favoriteProductService.getAll().pipe(
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
            .subscribe(() => {
                this.syncProductFavoriteState(product.id, false, null);
                this.loadFavorites();
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

type ProductListFilterState = {
    onlyMine: boolean;
    productTypes: ProductType[];
    caloriesFrom: number | null;
    caloriesTo: number | null;
    hasImage: boolean | null;
};

type ProductListFilterChanges = {
    productTypes: ProductType[];
    onlyMineChanged: boolean;
    typesChanged: boolean;
    caloriesChanged: boolean;
    imageChanged: boolean;
    hasChanges: boolean;
};
