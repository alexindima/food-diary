import { CommonModule } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { ChangeDetectionStrategy, Component, computed, DestroyRef, effect, ElementRef, inject, signal, viewChild } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormControl, FormGroup, ReactiveFormsModule } from '@angular/forms';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiHintDirective } from 'fd-ui-kit';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button.component';
import { FdUiDialogService } from 'fd-ui-kit/dialog/fd-ui-dialog.service';
import { FdUiIconComponent } from 'fd-ui-kit/icon/fd-ui-icon.component';
import { FdUiInputComponent } from 'fd-ui-kit/input/fd-ui-input.component';
import { FdUiPaginationComponent } from 'fd-ui-kit/pagination/fd-ui-pagination.component';
import { catchError, debounceTime, distinctUntilChanged, finalize, map, Observable, of, switchMap, tap } from 'rxjs';

import { BarcodeScannerComponent } from '../../../../components/shared/barcode-scanner/barcode-scanner.component';
import { ErrorStateComponent } from '../../../../components/shared/error-state/error-state.component';
import { FavoritesSectionComponent } from '../../../../components/shared/favorites-section/favorites-section.component';
import { PageBodyComponent } from '../../../../components/shared/page-body/page-body.component';
import { PageHeaderComponent } from '../../../../components/shared/page-header/page-header.component';
import { ProductCardComponent } from '../../../../components/shared/product-card/product-card.component';
import { SkeletonCardComponent } from '../../../../components/shared/skeleton-card/skeleton-card.component';
import { FdPageContainerDirective } from '../../../../directives/layout/page-container.directive';
import { NavigationService } from '../../../../services/navigation.service';
import { ViewportService } from '../../../../services/viewport.service';
import { FormGroupControls } from '../../../../shared/lib/common.data';
import { PagedData } from '../../../../shared/lib/paged-data.data';
import { QuickMealService } from '../../../meals/lib/quick-meal.service';
import { FavoriteProductService } from '../../api/favorite-product.service';
import { OpenFoodFactsProduct, OpenFoodFactsService } from '../../api/open-food-facts.service';
import { ProductService } from '../../api/product.service';
import { resolveProductImageUrl } from '../../lib/product-image.util';
import { buildProductTypeTranslationKey } from '../../lib/product-type.utils';
import { FavoriteProduct, Product, ProductFilters, ProductType } from '../../models/product.data';
import { ProductListFiltersDialogComponent, ProductListFiltersDialogResult } from './product-list-filters-dialog.component';

@Component({
    selector: 'fd-product-list-base',
    standalone: true,
    templateUrl: './product-list-base.component.html',
    styleUrls: ['./product-list-base.component.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush,
    imports: [
        CommonModule,
        ReactiveFormsModule,
        TranslatePipe,
        FdUiHintDirective,
        FdUiInputComponent,
        FdUiButtonComponent,
        FdUiPaginationComponent,
        SkeletonCardComponent,
        ErrorStateComponent,
        FdUiIconComponent,
        FavoritesSectionComponent,
        PageHeaderComponent,
        PageBodyComponent,
        FdPageContainerDirective,
        ProductCardComponent,
    ],
})
export class ProductListBaseComponent {
    protected readonly productService = inject(ProductService);
    protected readonly navigationService = inject(NavigationService);
    protected readonly pageSize = 10;
    protected readonly fdDialogService = inject(FdUiDialogService);
    protected readonly quickConsumptionService = inject(QuickMealService);
    protected readonly favoriteProductService = inject(FavoriteProductService);
    private readonly viewportService = inject(ViewportService);
    private readonly destroyRef = inject(DestroyRef);
    private readonly openFoodFactsService = inject(OpenFoodFactsService);

    private readonly header = viewChild.required<PageHeaderComponent, ElementRef>(PageHeaderComponent, { read: ElementRef });

    public searchForm: FormGroup<ProductSearchFormGroup>;
    public productData: PagedData<Product> = new PagedData<Product>();
    public currentPageIndex = 0;
    public recentProducts: Product[] = [];
    public readonly favorites = signal<FavoriteProduct[]>([]);
    public readonly favoriteTotalCount = signal(0);
    public readonly isFavoritesOpen = signal(false);
    public readonly isFavoritesLoadingMore = signal(false);
    public readonly errorKey = signal<string | null>(null);
    public readonly isMobileView = this.viewportService.isMobile;
    public readonly hasSearchValue = computed(() => !!this.searchForm.controls.search.value?.trim());
    public readonly showRecentSection = computed(() => !this.hasSearchValue() && this.recentProducts.length > 0);
    public readonly allProductsSectionItems = computed(() => {
        const products = this.productData.items();
        if (products.length === 0) {
            return [];
        }

        if (!this.showRecentSection()) {
            return products;
        }

        const recentIds = new Set(this.recentProducts.map(product => product.id));
        return products.filter(product => !recentIds.has(product.id));
    });
    public readonly hasVisibleProducts = computed(() => this.showRecentSection() || this.allProductsSectionItems().length > 0);
    public readonly hasActiveFilters = computed(() => this.searchForm.controls.onlyMine.value || this.selectedProductTypes().length > 0);
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
                debounceTime(300),
                switchMap(value => this.loadProducts(1, this.pageSize, value)),
                takeUntilDestroyed(this.destroyRef),
            )
            .subscribe();

        this.searchForm.controls.onlyMine.valueChanges
            .pipe(
                distinctUntilChanged(),
                switchMap(() => this.loadProducts(1, this.pageSize, this.searchForm.controls.search.value)),
                takeUntilDestroyed(this.destroyRef),
            )
            .subscribe();
    }

    public resolveImage(product: Product): string | undefined {
        return resolveProductImageUrl(product.imageUrl ?? undefined, product.productType ?? ProductType.Unknown);
    }

    protected isPrivateVisibility(visibility: Product['visibility']): boolean {
        return visibility?.toString().toUpperCase() === 'PRIVATE';
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
        void this.navigationService.navigateToProductAdd();
    }

    public openBarcodeScanner(): void {
        this.fdDialogService
            .open<BarcodeScannerComponent, null, string | null>(BarcodeScannerComponent, {
                size: 'lg',
            })
            .afterClosed()
            .subscribe(barcode => {
                if (barcode) {
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
            .subscribe(result => {
                if (!result) {
                    return;
                }

                const normalizedTypes = this.normalizeProductTypes(result.productTypes);
                const onlyMineChanged = currentOnlyMine !== result.onlyMine;
                const typesChanged = !this.areProductTypesEqual(currentTypes, normalizedTypes);

                if (!onlyMineChanged && !typesChanged) {
                    return;
                }

                if (typesChanged) {
                    this.selectedProductTypes.set(normalizedTypes);
                }

                if (onlyMineChanged) {
                    this.searchForm.controls.onlyMine.setValue(result.onlyMine);
                    return;
                }

                this.loadProducts(1, this.pageSize, this.searchForm.controls.search.value).subscribe();
            });
    }

    protected loadProducts(page: number, limit: number, search: string | null): Observable<void> {
        this.productData.setLoading(true);
        this.offProducts.set([]);
        const filters = new ProductFilters(search, this.selectedProductTypes());
        const includePublic = !this.searchForm.controls.onlyMine.value;

        this.searchOpenFoodFacts(search);

        return this.productService.query(page, limit, filters, includePublic).pipe(
            tap(data => {
                this.productData.setData(data);
                this.recentProducts = [];
                this.currentPageIndex = data.page - 1;
                this.errorKey.set(null);
            }),
            map(() => void 0),
            catchError((_error: HttpErrorResponse) => {
                this.productData.clearData();
                this.recentProducts = [];
                this.errorKey.set('ERRORS.LOAD_FAILED_TITLE');
                return of(void 0);
            }),
            finalize(() => this.productData.setLoading(false)),
        );
    }

    protected loadInitialOverview(): Observable<void> {
        this.productData.setLoading(true);
        this.offProducts.set([]);
        this.searchOpenFoodFacts(this.searchForm.controls.search.value);

        return this.productService.queryOverview(1, this.pageSize, undefined, true, 10, 10).pipe(
            tap(data => {
                this.productData.setData(data.allProducts);
                this.recentProducts = data.recentItems;
                this.favorites.set(data.favoriteItems);
                this.favoriteTotalCount.set(data.favoriteTotalCount);
                this.currentPageIndex = data.allProducts.page - 1;
                this.errorKey.set(null);
            }),
            map(() => void 0),
            catchError((_error: HttpErrorResponse) => {
                this.productData.clearData();
                this.recentProducts = [];
                this.favorites.set([]);
                this.favoriteTotalCount.set(0);
                this.errorKey.set('ERRORS.LOAD_FAILED_TITLE');
                return of(void 0);
            }),
            finalize(() => this.productData.setLoading(false)),
        );
    }

    private searchOpenFoodFacts(search: string | null): void {
        const trimmed = search?.trim();
        if (!trimmed || trimmed.length < 2) {
            this.offProducts.set([]);
            return;
        }

        this.offLoading.set(true);
        this.openFoodFactsService
            .search(trimmed, 5)
            .pipe(
                takeUntilDestroyed(this.destroyRef),
                finalize(() => this.offLoading.set(false)),
            )
            .subscribe(products => this.offProducts.set(products));
    }

    public onOffProductClick(offProduct: OpenFoodFactsProduct): void {
        void this.navigationService.navigateToProductAdd({
            state: {
                barcode: offProduct.barcode,
                offProduct,
            },
        });
    }

    protected scrollToTop(): void {
        this.header().nativeElement.scrollIntoView({ behavior: 'smooth', block: 'start' });
    }

    protected onProductClick(_product: Product): void | Promise<void> {}

    public onAddToMeal(product: Product): void {
        this.quickConsumptionService.addProduct(product);
    }

    public loadFavorites(): void {
        this.isFavoritesLoadingMore.set(true);
        this.favoriteProductService
            .getAll()
            .pipe(
                takeUntilDestroyed(this.destroyRef),
                finalize(() => this.isFavoritesLoadingMore.set(false)),
            )
            .subscribe(favorites => {
                this.favorites.set(favorites);
                this.favoriteTotalCount.set(favorites.length);
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
                if (product) {
                    void this.onProductClick(product);
                }
            });
    }

    public addFavoriteProductToMeal(favorite: FavoriteProduct): void {
        this.productService
            .getById(favorite.productId)
            .pipe(takeUntilDestroyed(this.destroyRef))
            .subscribe(product => {
                if (product) {
                    this.onAddToMeal(product);
                }
            });
    }

    public removeFavorite(favorite: FavoriteProduct): void {
        this.favoriteProductService.remove(favorite.id).subscribe({
            next: () => {
                this.loadFavorites();
                this.reloadCurrentPage();
                this.favoriteTotalCount.update(count => Math.max(0, count - 1));
                this.recentProducts = this.recentProducts.map(product =>
                    product.id === favorite.productId ? { ...product, isFavorite: false, favoriteProductId: null } : product,
                );
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
}

interface ProductSearchFormValues {
    search: string | null;
    onlyMine: boolean;
}

type ProductSearchFormGroup = FormGroupControls<ProductSearchFormValues>;
