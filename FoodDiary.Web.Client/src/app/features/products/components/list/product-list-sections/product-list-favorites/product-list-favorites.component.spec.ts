import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { TranslateModule } from '@ngx-translate/core';
import { describe, expect, it, vi } from 'vitest';

import type { FavoriteProduct } from '../../../../models/product.data';
import { ProductListFavoritesComponent } from './product-list-favorites.component';

describe('ProductListFavoritesComponent', () => {
    it('should render favorite products', async () => {
        const favorite = createFavoriteProduct();
        const { fixture } = await setupComponentAsync({ favorites: [favorite] });
        const element = fixture.nativeElement as HTMLElement;

        expect(element.textContent).toContain(favorite.name);
        expect(element.textContent).toContain(favorite.brand);
    });

    it('should emit favorite section actions', async () => {
        const favorite = createFavoriteProduct();
        const { component } = await setupComponentAsync({ favorites: [favorite] });
        const toggleHandler = vi.fn();
        const loadMoreHandler = vi.fn();
        const openHandler = vi.fn();
        const addHandler = vi.fn();
        const removeHandler = vi.fn();
        component.favoritesToggle.subscribe(toggleHandler);
        component.loadMore.subscribe(loadMoreHandler);
        component.openProduct.subscribe(openHandler);
        component.addToMeal.subscribe(addHandler);
        component.favoriteRemove.subscribe(removeHandler);

        component.favoritesToggle.emit();
        component.loadMore.emit();
        component.openProduct.emit(favorite);
        component.addToMeal.emit(favorite);
        component.favoriteRemove.emit(favorite);

        expect(toggleHandler).toHaveBeenCalled();
        expect(loadMoreHandler).toHaveBeenCalled();
        expect(openHandler).toHaveBeenCalledWith(favorite);
        expect(addHandler).toHaveBeenCalledWith(favorite);
        expect(removeHandler).toHaveBeenCalledWith(favorite);
    });
});

type ProductListFavoritesSetupOptions = {
    favorites?: FavoriteProduct[];
};

async function setupComponentAsync(
    options: ProductListFavoritesSetupOptions = {},
): Promise<{ component: ProductListFavoritesComponent; fixture: ComponentFixture<ProductListFavoritesComponent> }> {
    const favorites = options.favorites ?? [];

    await TestBed.configureTestingModule({
        imports: [ProductListFavoritesComponent, TranslateModule.forRoot()],
    }).compileComponents();

    const fixture = TestBed.createComponent(ProductListFavoritesComponent);
    fixture.componentRef.setInput('favorites', favorites);
    fixture.componentRef.setInput('totalCount', favorites.length);
    fixture.componentRef.setInput('isOpen', true);
    fixture.componentRef.setInput('isLoadingMore', false);
    fixture.detectChanges();

    return {
        component: fixture.componentInstance,
        fixture,
    };
}

function createFavoriteProduct(): FavoriteProduct {
    return {
        id: 'favorite-1',
        productId: 'product-1',
        name: 'Apple',
        createdAtUtc: '2026-04-05T10:30:00Z',
        productName: 'Apple',
        brand: 'Garden',
        caloriesPerBase: 52,
        baseUnit: 'G',
        defaultPortionAmount: 100,
    };
}
