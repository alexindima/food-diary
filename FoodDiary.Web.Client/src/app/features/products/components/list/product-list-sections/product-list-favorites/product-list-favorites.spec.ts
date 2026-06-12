import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { TranslateModule } from '@ngx-translate/core';
import { describe, expect, it, vi } from 'vitest';

import type { FavoriteProduct } from '../../../../models/product.data';
import { ProductListFavoritesComponent } from './product-list-favorites';

const DEFAULT_PORTION_AMOUNT = 100;
const ENTER_PORTION_AMOUNT = 125;
const BLUR_PORTION_AMOUNT = 130;

describe('ProductListFavoritesComponent', () => {
    it('should render favorite products', async () => {
        const favorite = createFavoriteProduct();
        const { fixture } = await setupComponentAsync({ favorites: [favorite] });
        const element = fixture.nativeElement as HTMLElement;

        expect(element.textContent).toContain(favorite.name);
        expect(element.textContent).toContain(favorite.brand);
        expect(element.textContent).toContain('100');
    });

    it('should emit favorite section actions', async () => {
        const favorite = createFavoriteProduct();
        const { component, fixture } = await setupComponentAsync({ favorites: [favorite] });
        const toggleHandler = vi.fn();
        const loadMoreHandler = vi.fn();
        const openHandler = vi.fn();
        const addHandler = vi.fn();
        const saveHandler = vi.fn();
        const removeHandler = vi.fn();
        component['favoritesToggle'].subscribe(toggleHandler);
        component['loadMore'].subscribe(loadMoreHandler);
        component['openProduct'].subscribe(openHandler);
        component['addToMeal'].subscribe(addHandler);
        component['preferredPortionSave'].subscribe(saveHandler);
        component['favoriteRemove'].subscribe(removeHandler);

        const input = getPortionInput(fixture);
        input.value = '150';
        input.dispatchEvent(new Event('input'));
        component['favoritesToggle'].emit();
        component['loadMore'].emit();
        component['openProduct'].emit(favorite);
        component['addToMeal'].emit(favorite);
        component['onPortionSave'](favorite);
        component['favoriteRemove'].emit(favorite);

        expect(toggleHandler).toHaveBeenCalled();
        expect(loadMoreHandler).toHaveBeenCalled();
        expect(openHandler).toHaveBeenCalledWith(favorite);
        expect(addHandler).toHaveBeenCalledWith(favorite);
        expect(saveHandler).toHaveBeenCalledWith({ favorite, preferredPortionAmount: 150 });
        expect(removeHandler).toHaveBeenCalledWith(favorite);
    });

    it('should save favorite portion on Enter and blur only when amount changes', async () => {
        const favorite = createFavoriteProduct();
        const { component, fixture } = await setupComponentAsync({ favorites: [favorite] });
        const saveHandler = vi.fn();
        component['preferredPortionSave'].subscribe(saveHandler);
        const input = getPortionInput(fixture);

        input.value = String(DEFAULT_PORTION_AMOUNT);
        input.dispatchEvent(new Event('input'));
        component['onPortionKeydown'](favorite, new KeyboardEvent('keydown', { key: 'Enter' }));

        input.value = String(ENTER_PORTION_AMOUNT);
        input.dispatchEvent(new Event('input'));
        component['onPortionKeydown'](favorite, new KeyboardEvent('keydown', { key: 'Enter' }));

        input.value = String(BLUR_PORTION_AMOUNT);
        input.dispatchEvent(new Event('input'));
        component['onPortionBlur'](favorite);

        expect(saveHandler).toHaveBeenCalledTimes(2);
        expect(saveHandler).toHaveBeenNthCalledWith(1, { favorite, preferredPortionAmount: ENTER_PORTION_AMOUNT });
        expect(saveHandler).toHaveBeenNthCalledWith(2, { favorite, preferredPortionAmount: BLUR_PORTION_AMOUNT });
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

function getPortionInput(fixture: ComponentFixture<ProductListFavoritesComponent>): HTMLInputElement {
    const host = fixture.nativeElement as HTMLElement;
    const input = host.querySelector<HTMLInputElement>('input');
    if (input === null) {
        throw new Error('Expected favorite portion input to be rendered.');
    }

    return input;
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
        preferredPortionAmount: DEFAULT_PORTION_AMOUNT,
        defaultPortionAmount: DEFAULT_PORTION_AMOUNT,
    };
}
