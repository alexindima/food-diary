import { ChangeDetectionStrategy, Component, ElementRef, inject, viewChild } from '@angular/core';
import { ReactiveFormsModule } from '@angular/forms';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiHintDirective } from 'fd-ui-kit';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button.component';
import { FdUiInputComponent } from 'fd-ui-kit/input/fd-ui-input.component';

import { ErrorStateComponent } from '../../../../../components/shared/error-state/error-state.component';
import { PageBodyComponent } from '../../../../../components/shared/page-body/page-body.component';
import { PageHeaderComponent } from '../../../../../components/shared/page-header/page-header.component';
import { SkeletonCardComponent } from '../../../../../components/shared/skeleton-card/skeleton-card.component';
import { FdPageContainerDirective } from '../../../../../directives/layout/page-container.directive';
import type { OpenFoodFactsProduct } from '../../../api/open-food-facts.service';
import type { FavoriteProduct, Product } from '../../../models/product.data';
import { ProductListFacade } from '../product-list-lib/product-list.facade';
import { ProductListEmptyStateComponent } from '../product-list-sections/product-list-empty-state/product-list-empty-state.component';
import { ProductListFavoritesComponent } from '../product-list-sections/product-list-favorites/product-list-favorites.component';
import { ProductListGroupsComponent } from '../product-list-sections/product-list-groups/product-list-groups.component';
import { ProductListOffSectionComponent } from '../product-list-sections/product-list-off-section/product-list-off-section.component';
import { ProductListPaginationComponent } from '../product-list-sections/product-list-pagination/product-list-pagination.component';

@Component({
    selector: 'fd-product-list-base',
    templateUrl: './product-list-base.component.html',
    styleUrls: ['./product-list-base.component.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush,
    providers: [ProductListFacade],
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
    protected readonly productListFacade = inject(ProductListFacade);
    private readonly header = viewChild.required<PageHeaderComponent, ElementRef<HTMLElement>>(PageHeaderComponent, { read: ElementRef });

    public readonly searchForm = this.productListFacade.searchForm;
    public readonly productData = this.productListFacade.productData;
    public readonly favorites = this.productListFacade.favorites;
    public readonly favoriteTotalCount = this.productListFacade.favoriteTotalCount;
    public readonly isFavoritesOpen = this.productListFacade.isFavoritesOpen;
    public readonly favoriteLoadingIds = this.productListFacade.favoriteLoadingIds;
    public readonly isFavoritesLoadingMore = this.productListFacade.isFavoritesLoadingMore;
    public readonly errorKey = this.productListFacade.errorKey;
    public readonly onlyMineFilter = this.productListFacade.onlyMineFilter;
    public readonly isMobileView = this.productListFacade.isMobileView;
    public readonly recentProductItems = this.productListFacade.recentProductItems;
    public readonly allProductItems = this.productListFacade.allProductItems;
    public readonly hasVisibleProducts = this.productListFacade.hasVisibleProducts;
    public readonly hasActiveFilters = this.productListFacade.hasActiveFilters;
    public readonly isEmptyState = this.productListFacade.isEmptyState;
    public readonly allProductsSectionLabelKey = this.productListFacade.allProductsSectionLabelKey;
    public readonly isMobileSearchVisible = this.productListFacade.isMobileSearchVisible;
    public readonly offProducts = this.productListFacade.offProducts;
    public readonly offLoading = this.productListFacade.offLoading;
    protected readonly pageSize = this.productListFacade.pageSize;
    protected readonly fdDialogService = this.productListFacade.fdDialogService;
    protected readonly navigationService = this.productListFacade.navigationService;

    public get currentPageIndex(): number {
        return this.productListFacade.currentPageIndex;
    }

    public resolveImage(product: Product): string | undefined {
        return this.productListFacade.resolveImage(product);
    }

    public retryLoad(): void {
        this.productListFacade.retryLoad();
    }

    public onPageChange(pageIndex: number): void {
        this.scrollToTop();
        this.productListFacade.onPageChange(pageIndex);
    }

    public onAddProductClick(): void {
        this.productListFacade.onAddProductClick();
    }

    public openBarcodeScanner(): void {
        this.productListFacade.openBarcodeScanner();
    }

    public toggleOnlyMine(): void {
        this.productListFacade.toggleOnlyMine();
    }

    public toggleMobileSearch(): void {
        this.productListFacade.toggleMobileSearch();
    }

    public openFilters(): void {
        this.productListFacade.openFilters();
    }

    public onOffProductClick(offProduct: OpenFoodFactsProduct): void {
        this.productListFacade.onOffProductClick(offProduct);
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
        this.productListFacade.onAddToMeal(product);
    }

    public loadFavorites(): void {
        this.productListFacade.loadFavorites();
    }

    public onProductFavoriteToggle(product: Product): void {
        this.productListFacade.onProductFavoriteToggle(product);
    }

    public toggleFavorites(): void {
        this.productListFacade.toggleFavorites();
    }

    public openFavoriteProduct(favorite: FavoriteProduct): void {
        this.productListFacade.openFavoriteProduct(favorite, product => {
            this.onProductClick(product);
        });
    }

    public addFavoriteProductToMeal(favorite: FavoriteProduct): void {
        this.productListFacade.addFavoriteProductToMeal(favorite);
    }

    public removeFavorite(favorite: FavoriteProduct): void {
        this.productListFacade.removeFavorite(favorite);
    }

    protected reloadCurrentPage(): void {
        this.productListFacade.reloadCurrentPage();
    }
}
