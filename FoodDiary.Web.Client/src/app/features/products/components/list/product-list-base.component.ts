import { BreakpointObserver } from '@angular/cdk/layout';
import { CommonModule } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { ChangeDetectionStrategy, Component, DestroyRef, ElementRef, inject, OnInit, signal, viewChild } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormControl, FormGroup, ReactiveFormsModule } from '@angular/forms';
import { TranslatePipe } from '@ngx-translate/core';
import { catchError, debounceTime, distinctUntilChanged, finalize, map, Observable, of, switchMap, tap } from 'rxjs';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button.component';
import { FdUiDialogService } from 'fd-ui-kit/dialog/fd-ui-dialog.service';
import { FdUiInputComponent } from 'fd-ui-kit/input/fd-ui-input.component';
import { ErrorStateComponent } from '../../../../components/shared/error-state/error-state.component';
import { SkeletonCardComponent } from '../../../../components/shared/skeleton-card/skeleton-card.component';
import { FdUiIconModule } from 'fd-ui-kit/material';
import { FdUiPaginationComponent } from 'fd-ui-kit/pagination/fd-ui-pagination.component';
import { BarcodeScannerComponent } from '../../../../components/shared/barcode-scanner/barcode-scanner.component';
import { PageBodyComponent } from '../../../../components/shared/page-body/page-body.component';
import { PageHeaderComponent } from '../../../../components/shared/page-header/page-header.component';
import { ProductCardComponent } from '../../../../components/shared/product-card/product-card.component';
import { FdPageContainerDirective } from '../../../../directives/layout/page-container.directive';
import { FavoriteProductService } from '../../api/favorite-product.service';
import { ProductService } from '../../api/product.service';
import { OpenFoodFactsProduct, OpenFoodFactsService } from '../../api/open-food-facts.service';
import { FavoriteProduct, Product, ProductFilters, ProductType } from '../../models/product.data';
import { NavigationService } from '../../../../services/navigation.service';
import { QuickMealService } from '../../../../features/meals/lib/quick-meal.service';
import { FormGroupControls } from '../../../../shared/lib/common.data';
import { PagedData } from '../../../../shared/lib/paged-data.data';
import { buildProductTypeTranslationKey } from '../../lib/product-type.utils';
import { resolveProductImageUrl } from '../../lib/product-image.util';
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
        FdUiInputComponent,
        FdUiButtonComponent,
        FdUiPaginationComponent,
        SkeletonCardComponent,
        ErrorStateComponent,
        FdUiIconModule,
        PageHeaderComponent,
        PageBodyComponent,
        FdPageContainerDirective,
        ProductCardComponent,
    ],
})
export class ProductListBaseComponent implements OnInit {
    protected readonly productService = inject(ProductService);
    protected readonly navigationService = inject(NavigationService);
    protected readonly pageSize = 10;
    protected readonly fdDialogService = inject(FdUiDialogService);
    protected readonly quickConsumptionService = inject(QuickMealService);
    protected readonly favoriteProductService = inject(FavoriteProductService);
    private readonly breakpointObserver = inject(BreakpointObserver);
    private readonly destroyRef = inject(DestroyRef);
    private readonly openFoodFactsService = inject(OpenFoodFactsService);

    private readonly header = viewChild.required<PageHeaderComponent, ElementRef>(PageHeaderComponent, { read: ElementRef });

    public searchForm: FormGroup<ProductSearchFormGroup>;
    public productData: PagedData<Product> = new PagedData<Product>();
    public currentPageIndex = 0;
    public recentProducts: Product[] = [];
    public readonly favorites = signal<FavoriteProduct[]>([]);
    public readonly isFavoritesOpen = signal(false);
    public readonly errorKey = signal<string | null>(null);
    public readonly isMobileView = signal<boolean>(window.matchMedia('(max-width: 768px)').matches);
    private readonly isMobileSearchOpen = signal(false);
    private readonly selectedProductTypes = signal<ProductType[]>([]);
    public readonly offProducts = signal<OpenFoodFactsProduct[]>([]);
    public readonly offLoading = signal(false);

    public constructor() {
        this.searchForm = new FormGroup<ProductSearchFormGroup>({
            search: new FormControl<string | null>(null),
            onlyMine: new FormControl<boolean>(false, { nonNullable: true }),
        });
    }

    public resolveImage(product: Product): string | undefined {
        return resolveProductImageUrl(product.imageUrl ?? undefined, product.productType ?? ProductType.Unknown);
    }

    protected isPrivateVisibility(visibility: Product['visibility']): boolean {
        return visibility?.toString().toUpperCase() === 'PRIVATE';
    }

    public ngOnInit(): void {
        this.breakpointObserver
            .observe('(max-width: 768px)')
            .pipe(
                map(result => result.matches),
                distinctUntilChanged(),
                takeUntilDestroyed(this.destroyRef),
            )
            .subscribe(isMobile => {
                this.isMobileView.set(isMobile);
                if (!isMobile) {
                    this.isMobileSearchOpen.set(false);
                }
            });

        this.loadProducts(1, this.pageSize, this.searchForm.controls.search.value).subscribe();

        this.searchForm.controls.search.valueChanges
            .pipe(
                debounceTime(300),
                switchMap(value => this.loadProducts(1, this.pageSize, value)),
            )
            .subscribe();

        this.searchForm.controls.onlyMine.valueChanges
            .pipe(
                distinctUntilChanged(),
                switchMap(() => this.loadProducts(1, this.pageSize, this.searchForm.controls.search.value)),
            )
            .subscribe();

        this.loadFavorites();
    }

    public retryLoad(): void {
        this.loadProducts(1, this.pageSize, this.searchForm.controls.search.value).subscribe();
    }

    public onPageChange(pageIndex: number): void {
        this.scrollToTop();

        this.currentPageIndex = pageIndex;
        this.loadProducts(this.currentPageIndex + 1, this.pageSize, this.searchForm.controls.search.value).subscribe();
    }

    public async onAddProductClick(): Promise<void> {
        await this.navigationService.navigateToProductAdd();
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
                size: 'sm',
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

        return this.productService.queryWithRecent(page, limit, filters, includePublic, 10).pipe(
            tap(data => {
                this.productData.setData(data.allProducts);
                this.recentProducts = data.recentItems;
                this.currentPageIndex = data.allProducts.page - 1;
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
        this.navigationService.navigateToProductAdd({
            state: {
                barcode: offProduct.barcode,
                offProduct,
            },
        });
    }

    protected scrollToTop(): void {
        this.header().nativeElement.scrollIntoView({ behavior: 'smooth', block: 'start' });
    }

    protected onProductClick(_product: Product): void {}

    public onAddToMeal(product: Product): void {
        this.quickConsumptionService.addProduct(product);
    }

    public loadFavorites(): void {
        this.favoriteProductService
            .getAll()
            .pipe(takeUntilDestroyed(this.destroyRef))
            .subscribe(favorites => {
                this.favorites.set(favorites);
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
                    this.onProductClick(product);
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
            },
        });
    }

    protected getProductTypeTranslationKey(product: Product): string {
        return buildProductTypeTranslationKey(product.productType ?? product.category ?? null);
    }

    public get showRecentSection(): boolean {
        return !this.hasSearchValue(this.searchForm.controls.search.value) && this.recentProducts.length > 0;
    }

    public get allProductsSectionItems(): Product[] {
        const products = this.productData.items();
        if (products.length === 0) {
            return [];
        }

        if (!this.showRecentSection) {
            return products;
        }

        const recentIds = new Set(this.recentProducts.map(product => product.id));
        return products.filter(product => !recentIds.has(product.id));
    }

    public get hasVisibleProducts(): boolean {
        return this.showRecentSection || this.allProductsSectionItems.length > 0;
    }

    public get hasActiveFilters(): boolean {
        return this.searchForm.controls.onlyMine.value || this.selectedProductTypes().length > 0;
    }

    public get isEmptyState(): boolean {
        return !this.hasVisibleProducts && !this.hasSearchValue(this.searchForm.controls.search.value) && !this.hasActiveFilters;
    }

    public get isNoResultsState(): boolean {
        return !this.hasVisibleProducts && !this.isEmptyState;
    }

    public get allProductsSectionLabelKey(): string {
        return this.hasSearchValue(this.searchForm.controls.search.value) ? 'PRODUCT_LIST.SEARCH_RESULTS' : 'PRODUCT_LIST.ALL_PRODUCTS';
    }

    public get isMobileSearchVisible(): boolean {
        return this.isMobileSearchOpen() || this.hasSearchValue(this.searchForm.controls.search.value);
    }

    protected reloadCurrentPage(): void {
        this.loadProducts(this.currentPageIndex + 1, this.pageSize, this.searchForm.controls.search.value).subscribe();
    }

    private hasSearchValue(value: string | null): boolean {
        return !!value?.trim();
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
