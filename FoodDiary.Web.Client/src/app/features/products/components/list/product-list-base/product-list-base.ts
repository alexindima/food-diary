import { ChangeDetectionStrategy, Component, computed, ElementRef, inject, viewChild } from '@angular/core';
import { FormField, FormRoot } from '@angular/forms/signals';
import { TranslatePipe } from '@ngx-translate/core';
import { FdTourService } from 'fd-tour';
import { FdUiHintDirective } from 'fd-ui-kit';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button';
import { FdUiInputComponent } from 'fd-ui-kit/input/fd-ui-input';

import { ErrorStateComponent } from '../../../../../components/shared/error-state/error-state';
import { PageBodyComponent } from '../../../../../components/shared/page-body/page-body';
import { PageHeaderComponent } from '../../../../../components/shared/page-header/page-header';
import { SkeletonCardComponent } from '../../../../../components/shared/skeleton-card/skeleton-card';
import { LocalizedTourDefinitionService } from '../../../../../shared/tours/localized-tour-definition.service';
import { FdPageContainerDirective } from '../../../../../shared/ui/layout/page-container.directive';
import { ProductListFacade } from '../../../lib/list/product-list.facade';
import { buildProductTypeTranslationKey } from '../../../lib/product-type.utils';
import type { OpenFoodFactsProduct } from '../../../models/open-food-facts.data';
import type { FavoriteProduct, Product } from '../../../models/product.data';
import { ProductListActiveFiltersComponent } from '../product-list-sections/product-list-active-filters/product-list-active-filters';
import { ProductListEmptyStateComponent } from '../product-list-sections/product-list-empty-state/product-list-empty-state';
import { ProductListFavoritesComponent } from '../product-list-sections/product-list-favorites/product-list-favorites';
import { ProductListGroupsComponent } from '../product-list-sections/product-list-groups/product-list-groups';
import { ProductListOffSectionComponent } from '../product-list-sections/product-list-off-section/product-list-off-section';
import { ProductListPaginationComponent } from '../product-list-sections/product-list-pagination/product-list-pagination';
import { PRODUCT_LIST_TOUR } from './product-list-tour';

@Component({
    selector: 'fd-product-list-base',
    templateUrl: './product-list-base.html',
    styleUrls: ['./product-list-base.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush,
    providers: [ProductListFacade],
    imports: [
        FormField,
        FormRoot,
        TranslatePipe,
        FdUiHintDirective,
        FdUiInputComponent,
        FdUiButtonComponent,
        SkeletonCardComponent,
        ErrorStateComponent,
        PageHeaderComponent,
        PageBodyComponent,
        FdPageContainerDirective,
        ProductListActiveFiltersComponent,
        ProductListFavoritesComponent,
        ProductListGroupsComponent,
        ProductListEmptyStateComponent,
        ProductListOffSectionComponent,
        ProductListPaginationComponent,
    ],
})
export class ProductListBaseComponent {
    protected readonly productListFacade = inject(ProductListFacade);
    private readonly tourService = inject(FdTourService);
    private readonly localizedTour = inject(LocalizedTourDefinitionService);
    private readonly header = viewChild.required<PageHeaderComponent, ElementRef<HTMLElement>>(PageHeaderComponent, { read: ElementRef });

    protected readonly searchForm = this.productListFacade.searchForm;
    protected readonly productData = this.productListFacade.productData;
    protected readonly favorites = this.productListFacade.favorites;
    protected readonly favoriteTotalCount = this.productListFacade.favoriteTotalCount;
    protected readonly isFavoritesOpen = this.productListFacade.isFavoritesOpen;
    protected readonly favoriteLoadingIds = this.productListFacade.favoriteLoadingIds;
    protected readonly isFavoritesLoadingMore = this.productListFacade.isFavoritesLoadingMore;
    protected readonly errorKey = this.productListFacade.errorKey;
    protected readonly onlyMineFilter = this.productListFacade.onlyMineFilter;
    protected readonly caloriesFromFilter = this.productListFacade.caloriesFromFilter;
    protected readonly caloriesToFilter = this.productListFacade.caloriesToFilter;
    protected readonly hasImageFilter = this.productListFacade.hasImageFilter;
    protected readonly isMobileView = this.productListFacade.isMobileView;
    protected readonly recentProductItems = this.productListFacade.recentProductItems;
    protected readonly allProductItems = this.productListFacade.allProductItems;
    protected readonly hasVisibleProducts = this.productListFacade.hasVisibleProducts;
    protected readonly hasActiveFilters = this.productListFacade.hasActiveFilters;
    protected readonly selectedProductTypeKeys = computed(() =>
        this.productListFacade.selectedProductTypes().map(type => buildProductTypeTranslationKey(type)),
    );
    protected readonly activeFilterKeys = computed(() => {
        const keys = [...this.selectedProductTypeKeys()];
        if (this.onlyMineFilter()) {
            keys.unshift('PRODUCT_LIST.FILTER_MY_PRODUCTS');
        }
        if (this.caloriesFromFilter() !== null || this.caloriesToFilter() !== null) {
            keys.push('PRODUCT_LIST.FILTER_CALORIES_ACTIVE');
        }
        if (this.hasImageFilter() === true) {
            keys.push('PRODUCT_LIST.FILTER_IMAGE_WITH');
        }
        if (this.hasImageFilter() === false) {
            keys.push('PRODUCT_LIST.FILTER_IMAGE_WITHOUT');
        }

        return keys;
    });
    protected readonly isEmptyState = this.productListFacade.isEmptyState;
    protected readonly allProductsSectionLabelKey = this.productListFacade.allProductsSectionLabelKey;
    protected readonly isMobileSearchVisible = this.productListFacade.isMobileSearchVisible;
    protected readonly offProducts = this.productListFacade.offProducts;
    protected readonly offLoading = this.productListFacade.offLoading;
    protected readonly pageSize = this.productListFacade.pageSize;
    protected readonly fdDialogService = this.productListFacade.fdDialogService;
    protected readonly navigationService = this.productListFacade.navigationService;

    protected get currentPageIndex(): number {
        return this.productListFacade.currentPageIndex;
    }

    protected resolveImage(product: Product): string | undefined {
        return this.productListFacade.resolveImage(product);
    }

    protected retryLoad(): void {
        this.productListFacade.retryLoad();
    }

    protected onPageChange(pageIndex: number): void {
        this.scrollToTop();
        this.productListFacade.onPageChange(pageIndex);
    }

    protected onAddProductClick(): void {
        this.productListFacade.onAddProductClick();
    }

    protected openBarcodeScanner(): void {
        this.productListFacade.openBarcodeScanner();
    }

    protected toggleOnlyMine(): void {
        this.productListFacade.toggleOnlyMine();
    }

    protected toggleMobileSearch(): void {
        this.productListFacade.toggleMobileSearch();
    }

    protected openFilters(): void {
        this.productListFacade.openFilters();
    }

    protected startProductListTour(force = true): void {
        this.tourService.start(this.localizedTour.build(PRODUCT_LIST_TOUR), { force });
    }

    protected onOffProductClick(offProduct: OpenFoodFactsProduct): void {
        this.productListFacade.onOffProductClick(offProduct);
    }

    protected scrollToTop(): void {
        this.header().nativeElement.scrollIntoView({ behavior: 'smooth', block: 'start' });
    }

    protected onProductClick(product: Product): void {
        this.handleProductClick(product);
    }

    protected handleProductClick(_product: Product): void {}

    protected onAddToMeal(product: Product): void {
        this.productListFacade.onAddToMeal(product);
    }

    protected loadFavorites(): void {
        this.productListFacade.loadFavorites();
    }

    protected onProductFavoriteToggle(product: Product): void {
        this.productListFacade.onProductFavoriteToggle(product);
    }

    protected toggleFavorites(): void {
        this.productListFacade.toggleFavorites();
    }

    protected openFavoriteProduct(favorite: FavoriteProduct): void {
        this.productListFacade.openFavoriteProduct(favorite, product => {
            this.onProductClick(product);
        });
    }

    protected addFavoriteProductToMeal(favorite: FavoriteProduct): void {
        this.productListFacade.addFavoriteProductToMeal(favorite);
    }

    protected removeFavorite(favorite: FavoriteProduct): void {
        this.productListFacade.removeFavorite(favorite);
    }

    protected reloadCurrentPage(): void {
        this.productListFacade.reloadCurrentPage();
    }
}
