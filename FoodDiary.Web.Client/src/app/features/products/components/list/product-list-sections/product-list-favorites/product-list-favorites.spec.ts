import { signal } from '@angular/core';
import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { FdUiDialogService } from 'fd-ui-kit/dialog/fd-ui-dialog.service';
import { describe, expect, it, vi } from 'vitest';

import { provideTranslateTesting } from '../../../../../../../testing/translate-testing.module';
import { AuthService } from '../../../../../../services/auth.service';
import type { FavoriteProduct } from '../../../../models/product.data';
import { ProductListFavoritesComponent } from './product-list-favorites';

const DEFAULT_PORTION_AMOUNT = 100;

describe('ProductListFavoritesComponent', () => {
    it('should render favorite products', async () => {
        const favorite = createFavoriteProduct();
        const { fixture } = await setupComponentAsync({ favorites: [favorite] });
        const element = fixture.nativeElement as HTMLElement;

        expect(element.textContent).toContain(favorite.name);
        expect(element.textContent).toContain(favorite.brand);
        expect(element.querySelector('.product-list__favorite-portion-row')).toBeNull();
        expect(element.querySelector('input')).toBeNull();
    });

    it('should emit favorite section actions', async () => {
        const favorite = createFavoriteProduct();
        const { component } = await setupComponentAsync({ favorites: [favorite] });
        const toggleHandler = vi.fn();
        const loadMoreHandler = vi.fn();
        const openHandler = vi.fn();
        const addHandler = vi.fn();
        const removeHandler = vi.fn();
        component['favoritesToggle'].subscribe(toggleHandler);
        component['loadMore'].subscribe(loadMoreHandler);
        component['openProduct'].subscribe(openHandler);
        component['addToMeal'].subscribe(addHandler);
        component['favoriteRemove'].subscribe(removeHandler);

        component['favoritesToggle'].emit();
        component['loadMore'].emit();
        component['openProduct'].emit(favorite);
        component['addToMeal'].emit(favorite);
        component['favoriteRemove'].emit(favorite);

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
        imports: [ProductListFavoritesComponent],
        providers: [
            provideTranslateTesting(),
            {
                provide: AuthService,
                useValue: {
                    isAuthenticated: signal(true),
                },
            },
            {
                provide: FdUiDialogService,
                useValue: {
                    open: vi.fn(),
                },
            },
        ],
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
        barcode: '1234567890123',
        comment: 'Fresh',
        imageUrl: null,
        caloriesPerBase: 52,
        proteinsPerBase: 1,
        fatsPerBase: 2,
        carbsPerBase: 3,
        fiberPerBase: 4,
        alcoholPerBase: 0,
        qualityScore: 72,
        qualityGrade: 'green',
        isOwnedByCurrentUser: true,
        baseUnit: 'G',
        preferredPortionAmount: DEFAULT_PORTION_AMOUNT,
        defaultPortionAmount: DEFAULT_PORTION_AMOUNT,
    };
}
