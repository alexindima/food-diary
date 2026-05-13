import { computed, DestroyRef, effect, inject, Injectable, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormControl, FormGroup } from '@angular/forms';
import { FdUiDialogService } from 'fd-ui-kit/dialog/fd-ui-dialog.service';
import { catchError, debounceTime, distinctUntilChanged, EMPTY, finalize, map, type Observable, of, switchMap, take, tap } from 'rxjs';

import { BarcodeScannerComponent } from '../../../../../components/shared/barcode-scanner/barcode-scanner.component';
import { APP_SEARCH_DEBOUNCE_MS } from '../../../../../config/runtime-ui.tokens';
import { NavigationService } from '../../../../../services/navigation.service';
import { ViewportService } from '../../../../../services/viewport.service';
import type { FormGroupControls } from '../../../../../shared/lib/common.data';
import { PagedData } from '../../../../../shared/lib/paged-data.data';
import { QuickMealService } from '../../../../meals/lib/quick-meal.service';
import { FavoriteProductService } from '../../../api/favorite-product.service';
import { type OpenFoodFactsProduct, OpenFoodFactsService } from '../../../api/open-food-facts.service';
import { ProductService } from '../../../api/product.service';
import { resolveProductImageUrl } from '../../../lib/product-image.util';
import { type FavoriteProduct, type Product, ProductFilters, ProductType } from '../../../models/product.data';
import {
    PRODUCT_LIST_FAVORITE_LIMIT,
    PRODUCT_LIST_OFF_SEARCH_LIMIT,
    PRODUCT_LIST_OFF_SEARCH_MIN_LENGTH,
    PRODUCT_LIST_PAGE_SIZE,
    PRODUCT_LIST_RECENT_LIMIT,
} from '../product-list.config';
import type { ProductCardViewModel } from '../product-list.types';
import { ProductListFiltersDialogComponent } from '../product-list-filters-dialog/product-list-filters-dialog.component';
import type { ProductListFiltersDialogResult } from '../product-list-filters-dialog/product-list-filters-dialog.types';

@Injectable({ providedIn: 'root' })
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
    public readonly searchForm = new FormGroup<ProductSearchFormGroup>({
        search: new FormControl<string | null>(null),
        onlyMine: new FormControl<boolean>(false, { nonNullable: true }),
    });
    public readonly productData = new PagedData<Product>();
    public currentPageIndex = 0;
    public readonly recentProducts = signal<Product[]>([]);
    public readonly favorites = signal<FavoriteProduct[]>([]);
    public readonly favoriteTotalCount = signal(0);
    public readonly isFavoritesOpen = signal(false);
    public readonly favoriteLoadingIds = signal<ReadonlySet<string>>(new Set<string>());
    public readonly isFavoritesLoadingMore = signal(false);
    public readonly errorKey = signal<string | null>(null);
    public readonly searchValue = signal<string | null>(null);
    public readonly onlyMineFilter = signal(false);
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
    public readonly hasVisibleProducts = computed(() => this.showRecentSection() || this.allProductsSectionItems().length > 0);
    public readonly hasActiveFilters = computed(() => this.onlyMineFilter() || this.selectedProductTypes().length > 0);
    public readonly isEmptyState = computed(() => !this.hasVisibleProducts() && !this.hasSearchValue() && !this.hasActiveFilters());
    public readonly allProductsSectionLabelKey = computed(() =>
        this.hasSearchValue() ? 'PRODUCT_LIST.SEARCH_RESULTS' : 'PRODUCT_LIST.ALL_PRODUCTS',
    );
    public readonly isMobileSearchVisible = computed(() => this.isMobileSearchOpen() || this.hasSearchValue());
    public readonly offProducts = signal<OpenFoodFactsProduct[]>([]);
    public readonly offLoading = signal(false);
    private readonly isMobileSearchOpen = signal(false);
    private readonly selectedProductTypes = signal<ProductType[]>([]);

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
        this.loadProducts(this.currentPageIndex + 1, this.pageSize, this.searchForm.controls.search.value).subscribe();
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
                    this.searchForm.controls.search.setValue(barcode);
                }
            });
    }

    public toggleOnlyMine(): void {
        const control = this.searchForm.controls.onlyMine;
        control.setValue(!control.value);
    }

    public toggleMobileSearch(): void {
        this.isMobileSearchOpen.update(value => !value);
    }

    public openFilters(): void {
        const currentOnlyMine = this.searchForm.controls.onlyMine.value;
        const currentTypes = this.selectedProductTypes();

        this.fdDialogService
            .open<
                ProductListFiltersDialogComponent,
                {
                    onlyMine: boolean;
                    productTypes: ProductType[];
                },
                ProductListFiltersDialogResult | null
            >(ProductListFiltersDialogComponent, {
                preset: 'form',
                data: {
                    onlyMine: currentOnlyMine,
                    productTypes: [...currentTypes],
                },
            })
            .afterClosed()
            .pipe(
                switchMap(result => {
                    if (result === null || result === undefined) {
                        return EMPTY;
                    }

                    const normalizedTypes = this.normalizeProductTypes(result.productTypes);
                    const onlyMineChanged = currentOnlyMine !== result.onlyMine;
                    const typesChanged = !this.areProductTypesEqual(currentTypes, normalizedTypes);

                    if (!onlyMineChanged && !typesChanged) {
                        return EMPTY;
                    }

                    if (typesChanged) {
                        this.selectedProductTypes.set(normalizedTypes);
                    }

                    if (onlyMineChanged) {
                        this.searchForm.controls.onlyMine.setValue(result.onlyMine);
                        return EMPTY;
                    }

                    return this.loadProducts(1, this.pageSize, this.searchForm.controls.search.value);
                }),
                takeUntilDestroyed(this.destroyRef),
            )
            .subscribe();
    }

    public loadProducts(page: number, limit: number, search: string | null): Observable<void> {
        this.productData.setLoading(true);
        this.offProducts.set([]);
        const filters = new ProductFilters(search, this.selectedProductTypes());
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
        this.searchOpenFoodFacts(this.searchForm.controls.search.value);

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
            .add(product.id, product.name)
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
        this.productService
            .getById(favorite.productId)
            .pipe(takeUntilDestroyed(this.destroyRef))
            .subscribe(product => {
                if (product !== null) {
                    this.onAddToMeal(product);
                }
            });
    }

    public removeFavorite(favorite: FavoriteProduct): void {
        this.favoriteProductService
            .remove(favorite.id)
            .pipe(take(1))
            .subscribe({
                next: () => {
                    this.favorites.update(favorites => favorites.filter(item => item.id !== favorite.id));
                    this.favoriteTotalCount.update(count => Math.max(0, count - 1));
                    this.syncProductFavoriteState(favorite.productId, false, null);
                },
            });
    }

    public reloadCurrentPage(): void {
        this.loadProducts(this.currentPageIndex + 1, this.pageSize, this.searchForm.controls.search.value).subscribe();
    }

    public deleteProductAndReload(productId: string): Observable<void> {
        this.productData.setLoading(true);
        return this.productService
            .deleteById(productId)
            .pipe(switchMap(() => this.loadProducts(this.currentPageIndex + 1, this.pageSize, this.searchForm.controls.search.value)));
    }

    private bindSearch(): void {
        this.searchForm.controls.search.valueChanges
            .pipe(
                tap(value => {
                    this.searchValue.set(value);
                }),
                debounceTime(this.searchDebounceMs),
                distinctUntilChanged(),
                switchMap(value => this.loadProducts(1, this.pageSize, value)),
                takeUntilDestroyed(this.destroyRef),
            )
            .subscribe();

        this.searchForm.controls.onlyMine.valueChanges
            .pipe(
                distinctUntilChanged(),
                tap(value => {
                    this.onlyMineFilter.set(value);
                }),
                switchMap(() => this.loadProducts(1, this.pageSize, this.searchForm.controls.search.value)),
                takeUntilDestroyed(this.destroyRef),
            )
            .subscribe();
    }

    private searchOpenFoodFacts(search: string | null): void {
        const trimmed = search?.trim();
        if (trimmed === undefined || trimmed.length < PRODUCT_LIST_OFF_SEARCH_MIN_LENGTH) {
            this.offProducts.set([]);
            return;
        }

        this.offLoading.set(true);
        this.openFoodFactsService
            .search(trimmed, PRODUCT_LIST_OFF_SEARCH_LIMIT)
            .pipe(
                takeUntilDestroyed(this.destroyRef),
                finalize(() => {
                    this.offLoading.set(false);
                }),
            )
            .subscribe(products => {
                this.offProducts.set(products);
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

    private syncProductFavoriteState(productId: string, isFavorite: boolean, favoriteProductId: string | null): void {
        this.productData.items.update(items =>
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

type ProductSearchFormGroup = FormGroupControls<ProductSearchFormValues>;
