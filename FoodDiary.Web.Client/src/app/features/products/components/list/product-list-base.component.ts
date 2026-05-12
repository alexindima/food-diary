import { ChangeDetectionStrategy, Component, computed, DestroyRef, effect, ElementRef, inject, signal, viewChild } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormControl, FormGroup, ReactiveFormsModule } from '@angular/forms';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiHintDirective } from 'fd-ui-kit';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button.component';
import { FdUiDialogService } from 'fd-ui-kit/dialog/fd-ui-dialog.service';
import { FdUiInputComponent } from 'fd-ui-kit/input/fd-ui-input.component';
import { catchError, debounceTime, distinctUntilChanged, EMPTY, finalize, map, type Observable, of, switchMap, tap } from 'rxjs';

import { BarcodeScannerComponent } from '../../../../components/shared/barcode-scanner/barcode-scanner.component';
import { ErrorStateComponent } from '../../../../components/shared/error-state/error-state.component';
import { PageBodyComponent } from '../../../../components/shared/page-body/page-body.component';
import { PageHeaderComponent } from '../../../../components/shared/page-header/page-header.component';
import { SkeletonCardComponent } from '../../../../components/shared/skeleton-card/skeleton-card.component';
import { FdPageContainerDirective } from '../../../../directives/layout/page-container.directive';
import { NavigationService } from '../../../../services/navigation.service';
import { ViewportService } from '../../../../services/viewport.service';
import type { FormGroupControls } from '../../../../shared/lib/common.data';
import { PagedData } from '../../../../shared/lib/paged-data.data';
import { QuickMealService } from '../../../meals/lib/quick-meal.service';
import { FavoriteProductService } from '../../api/favorite-product.service';
import { type OpenFoodFactsProduct, OpenFoodFactsService } from '../../api/open-food-facts.service';
import { ProductService } from '../../api/product.service';
import { resolveProductImageUrl } from '../../lib/product-image.util';
import { buildProductTypeTranslationKey } from '../../lib/product-type.utils';
import { type FavoriteProduct, type Product, ProductFilters, ProductType } from '../../models/product.data';
import type { ProductCardViewModel } from './product-list.types';
import { ProductListEmptyStateComponent } from './product-list-empty-state.component';
import { ProductListFavoritesComponent } from './product-list-favorites.component';
import { ProductListFiltersDialogComponent, type ProductListFiltersDialogResult } from './product-list-filters-dialog.component';
import { ProductListGroupsComponent } from './product-list-groups.component';
import { ProductListOffSectionComponent } from './product-list-off-section.component';
import { ProductListPaginationComponent } from './product-list-pagination.component';

const DEFAULT_PAGE_SIZE = 10;
const SEARCH_DEBOUNCE_MS = 300;
const OFF_SEARCH_LIMIT = 5;

@Component({
    selector: 'fd-product-list-base',
    standalone: true,
    templateUrl: './product-list-base.component.html',
    styleUrls: ['./product-list-base.component.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush,
    imports: [
        ReactiveFormsModule,
        TranslatePipe,
        FdUiHintDirective,
        FdUiInputComponent,
        FdUiButtonComponent,
        SkeletonCardComponent,
        ErrorStateComponent,
        PageHeaderComponent,
        PageBodyComponent,
        FdPageContainerDirective,
        ProductListFavoritesComponent,
        ProductListGroupsComponent,
        ProductListEmptyStateComponent,
        ProductListOffSectionComponent,
        ProductListPaginationComponent,
    ],
})
export class ProductListBaseComponent {
    protected readonly productService = inject(ProductService);
    protected readonly navigationService = inject(NavigationService);
    protected readonly pageSize = DEFAULT_PAGE_SIZE;
    protected readonly fdDialogService = inject(FdUiDialogService);
    protected readonly quickConsumptionService = inject(QuickMealService);
    protected readonly favoriteProductService = inject(FavoriteProductService);
    private readonly viewportService = inject(ViewportService);
    private readonly destroyRef = inject(DestroyRef);
    private readonly openFoodFactsService = inject(OpenFoodFactsService);

    private readonly header = viewChild.required<PageHeaderComponent, ElementRef<HTMLElement>>(PageHeaderComponent, { read: ElementRef });

    public searchForm: FormGroup<ProductSearchFormGroup>;
    public productData: PagedData<Product> = new PagedData<Product>();
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
    public readonly recentProductItems = computed<ProductCardViewModel[]>(() =>
        this.recentProducts().map(product => ({
            product,
            imageUrl: this.resolveImage(product),
        })),
    );
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
    public readonly isNoResultsState = computed(() => !this.hasVisibleProducts() && !this.isEmptyState());
    public readonly allProductsSectionLabelKey = computed(() =>
        this.hasSearchValue() ? 'PRODUCT_LIST.SEARCH_RESULTS' : 'PRODUCT_LIST.ALL_PRODUCTS',
    );
    public readonly isMobileSearchVisible = computed(() => this.isMobileSearchOpen() || this.hasSearchValue());
    public readonly hasMoreFavorites = computed(() => this.favoriteTotalCount() > this.favorites().length);
    private readonly isMobileSearchOpen = signal(false);
    private readonly selectedProductTypes = signal<ProductType[]>([]);
    public readonly offProducts = signal<OpenFoodFactsProduct[]>([]);
    public readonly offLoading = signal(false);

    public constructor() {
        this.searchForm = new FormGroup<ProductSearchFormGroup>({
            search: new FormControl<string | null>(null),
            onlyMine: new FormControl<boolean>(false, { nonNullable: true }),
        });

        effect(() => {
            if (!this.isMobileView()) {
                this.isMobileSearchOpen.set(false);
            }
        });

        this.loadInitialOverview().subscribe();

        this.searchForm.controls.search.valueChanges
            .pipe(
                tap(value => {
                    this.searchValue.set(value);
                }),
                debounceTime(SEARCH_DEBOUNCE_MS),
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

    public resolveImage(product: Product): string | undefined {
        return resolveProductImageUrl(product.imageUrl ?? undefined, product.productType ?? ProductType.Unknown);
    }

    protected isPrivateVisibility(visibility: Product['visibility']): boolean {
        return visibility.toString().toUpperCase() === 'PRIVATE';
    }

    public retryLoad(): void {
        this.loadInitialOverview().subscribe();
    }

    public onPageChange(pageIndex: number): void {
        this.scrollToTop();

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

    protected loadProducts(page: number, limit: number, search: string | null): Observable<void> {
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

    protected loadInitialOverview(): Observable<void> {
        this.productData.setLoading(true);
        this.offProducts.set([]);
        this.searchOpenFoodFacts(this.searchForm.controls.search.value);

        return this.productService
            .queryOverview({ page: 1, limit: this.pageSize, includePublic: true, recentLimit: 10, favoriteLimit: 10 })
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

    private searchOpenFoodFacts(search: string | null): void {
        const trimmed = search?.trim();
        if (trimmed === undefined || trimmed.length < 2) {
            this.offProducts.set([]);
            return;
        }

        this.offLoading.set(true);
        this.openFoodFactsService
            .search(trimmed, OFF_SEARCH_LIMIT)
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

    public onOffProductClick(offProduct: OpenFoodFactsProduct): void {
        void this.navigationService.navigateToProductAddAsync({
            state: {
                barcode: offProduct.barcode,
                offProduct,
            },
        });
    }

    protected scrollToTop(): void {
        this.header().nativeElement.scrollIntoView({ behavior: 'smooth', block: 'start' });
    }

    public onProductClick(product: Product): void {
        void this.handleProductClickAsync(product);
    }

    protected async handleProductClickAsync(_product: Product): Promise<void> {
        return Promise.resolve();
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

    public openFavoriteProduct(favorite: FavoriteProduct): void {
        this.productService
            .getById(favorite.productId)
            .pipe(takeUntilDestroyed(this.destroyRef))
            .subscribe(product => {
                if (product !== null) {
                    this.onProductClick(product);
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
        this.favoriteProductService.remove(favorite.id).subscribe({
            next: () => {
                this.favorites.update(favorites => favorites.filter(item => item.id !== favorite.id));
                this.favoriteTotalCount.update(count => Math.max(0, count - 1));
                this.syncProductFavoriteState(favorite.productId, false, null);
            },
        });
    }

    protected getProductTypeTranslationKey(product: Product): string {
        return buildProductTypeTranslationKey(product.productType ?? product.category ?? null);
    }

    protected reloadCurrentPage(): void {
        this.loadProducts(this.currentPageIndex + 1, this.pageSize, this.searchForm.controls.search.value).subscribe();
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

interface ProductSearchFormValues {
    search: string | null;
    onlyMine: boolean;
}

type ProductSearchFormGroup = FormGroupControls<ProductSearchFormValues>;
