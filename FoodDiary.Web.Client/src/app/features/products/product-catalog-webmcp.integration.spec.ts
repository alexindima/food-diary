import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { provideRouter, Router, withComponentInputBinding } from '@angular/router';
import { RouterTestingHarness } from '@angular/router/testing';
import { WebmcpScreenRegistry } from 'fd-webmcp';
import { of, Subject } from 'rxjs';
import { describe, expect, it, type Mock, vi } from 'vitest';

import { provideTranslateTesting } from '../../../testing/translate-testing.module';
import { APP_SEARCH_DEBOUNCE_MS } from '../../config/runtime-ui.tokens';
import { AuthService } from '../../services/auth.service';
import { NavigationService } from '../../services/navigation.service';
import { DEVELOPMENT_SCREENS, type DevelopmentScreen } from '../../shared/development/development-tools';
import type { PageOf } from '../../shared/models/page-of.data';
import { MeasurementUnit, type Product, ProductType, ProductVisibility } from '../../shared/models/product.data';
import { entityId } from '../../shared/models/semantics/entity-id';
import { ViewportService } from '../../shared/platform/viewport.service';
import { QuickMealService } from '../meals/contracts/quick-meal';
import { USDA_PRODUCT_LINK } from '../usda/contracts/usda-product-link';
import { FavoriteProductService } from './api/favorite-product.service';
import { OpenFoodFactsService } from './api/open-food-facts.service';
import { ProductService } from './api/product.service';
import productRoutes from './product.routes';

const CATALOG_URL = '/products?campaign=manual';
const PAGE_SIZE = 10;
const OWNING_RESULT = 'Pear slices';
const PREVIOUS_RESULT = 'Oat flakes';
const CALORIE_LIMIT = 120;

const previousProduct: Product = {
    id: entityId<'product'>('fixture-old-oats'),
    name: PREVIOUS_RESULT,
    baseUnit: MeasurementUnit.G,
    productType: ProductType.Other,
    baseAmount: 100,
    defaultPortionAmount: 100,
    caloriesPerBase: 110,
    proteinsPerBase: 4,
    fatsPerBase: 3,
    carbsPerBase: 20,
    fiberPerBase: 0,
    alcoholPerBase: 0,
    usageCount: 0,
    visibility: ProductVisibility.Private,
    qualityScore: 80,
    qualityGrade: 'green',
    createdAt: new Date('2026-05-01T00:00:00Z'),
    isOwnedByCurrentUser: true,
};
const currentProduct: Product = { ...previousProduct, id: entityId<'product'>('fixture-pear-slices'), name: OWNING_RESULT };

type CatalogScreen = DevelopmentScreen & {
    setFiltersAsync: NonNullable<DevelopmentScreen['setFiltersAsync']>;
    openEntityAsync: NonNullable<DevelopmentScreen['openEntityAsync']>;
};

function configureCatalog(): {
    registry: WebmcpScreenRegistry;
    query: Mock<ProductService['query']>;
    getById: Mock<ProductService['getById']>;
} {
    const registry = new WebmcpScreenRegistry();
    const query = vi.fn<ProductService['query']>().mockReturnValue(of(productPage(currentProduct)));
    const getById = vi
        .fn<ProductService['getById']>()
        .mockImplementation(id => of(id === currentProduct.id ? currentProduct : previousProduct));
    const queryOverview = vi
        .fn<ProductService['queryOverview']>()
        .mockReturnValue(of({ allProducts: productPage(previousProduct), recentItems: [], favoriteItems: [], favoriteTotalCount: 0 }));
    TestBed.configureTestingModule({
        providers: [
            provideRouter([{ path: 'products', children: productRoutes }], withComponentInputBinding()),
            provideTranslateTesting(),
            provideHttpClient(),
            provideHttpClientTesting(),
            { provide: DEVELOPMENT_SCREENS, useValue: registry },
            { provide: APP_SEARCH_DEBOUNCE_MS, useValue: 0 },
            { provide: ProductService, useValue: { queryOverview, query, getById } },
            { provide: OpenFoodFactsService, useValue: { search: vi.fn().mockReturnValue(of([])) } },
            { provide: NavigationService, useValue: {} },
            { provide: FavoriteProductService, useValue: { getLookupPage: vi.fn().mockReturnValue(of([])) } },
            { provide: AuthService, useValue: { isAuthenticated: signal(true) } },
            { provide: ViewportService, useValue: { isMobile: signal(false) } },
            { provide: QuickMealService, useValue: { addProduct: vi.fn() } },
            { provide: USDA_PRODUCT_LINK, useValue: { getFoodDetail: vi.fn(), linkProduct: vi.fn(), unlinkProduct: vi.fn() } },
        ],
    });
    return { registry, query, getById };
}

function productPage(product: Product): PageOf<Product> {
    return { data: [product], page: 1, limit: PAGE_SIZE, totalItems: 1, totalPages: 1 };
}

function catalogScreen(registry: WebmcpScreenRegistry): CatalogScreen {
    const screen = registry.current();
    if (screen?.kind !== 'product-list' || screen.setFiltersAsync === undefined || screen.openEntityAsync === undefined) {
        throw new Error('Expected the registered product catalog screen');
    }
    return { ...screen, setFiltersAsync: screen.setFiltersAsync, openEntityAsync: screen.openEntityAsync };
}

describe('Product catalog WebMCP route', () => {
    it('applies published filters and rejects an unknown argument without altering the current URL', async () => {
        const { registry, query } = configureCatalog();
        const harness = await RouterTestingHarness.create(CATALOG_URL);
        const router = TestBed.inject(Router);
        const screen = catalogScreen(registry);
        expect(registry.read()).toMatchObject({
            kind: 'product-list',
            dirty: false,
            actions: { setFiltersAsync: true, openEntityAsync: true, fillDraft: false },
            data: {
                filterFields: ['search', 'page', 'onlyMine', 'types', 'caloriesFrom', 'caloriesTo', 'hasImage'],
                items: [{ id: previousProduct.id, name: PREVIOUS_RESULT, unit: MeasurementUnit.G }],
                page: 1,
                totalItems: 1,
                totalPages: 1,
            },
        });

        await expect(screen.setFiltersAsync({ visibilityOverride: true })).rejects.toThrow('Unexpected arguments');
        expect(router.url).toBe(CATALOG_URL);
        expect(query).not.toHaveBeenCalled();

        await screen.setFiltersAsync({ search: '  Pear  ', onlyMine: true, types: ProductType.Other, caloriesTo: CALORIE_LIMIT });
        harness.detectChanges();
        expect(router.parseUrl(router.url).queryParams).toEqual({
            campaign: 'manual',
            search: 'Pear',
            onlyMine: 'true',
            types: ProductType.Other,
            caloriesTo: String(CALORIE_LIMIT),
        });
        expect(query).toHaveBeenCalledExactlyOnceWith(
            1,
            PAGE_SIZE,
            expect.objectContaining({ search: 'Pear', productTypes: [ProductType.Other], caloriesTo: CALORIE_LIMIT }),
            expect.objectContaining({ includePublic: false }),
        );
        expect(harness.routeNativeElement?.textContent).toContain(OWNING_RESULT);
        expect(registry.read()?.data).toMatchObject({
            filters: { search: 'Pear', onlyMine: true, types: [ProductType.Other], caloriesTo: CALORIE_LIMIT },
            items: [{ id: currentProduct.id, name: OWNING_RESULT, unit: MeasurementUnit.G }],
            loading: false,
        });
    });

    it('opens the current rendered product after filtering and refuses the previous product while results refresh', async () => {
        const { registry, query, getById } = configureCatalog();
        const filtered = new Subject<PageOf<Product>>();
        query.mockReturnValueOnce(filtered);
        const harness = await RouterTestingHarness.create(CATALOG_URL);
        const router = TestBed.inject(Router);
        const screen = catalogScreen(registry);
        expect(harness.routeNativeElement?.textContent).toContain(PREVIOUS_RESULT);

        await screen.setFiltersAsync({ search: 'Pear' });
        harness.detectChanges();
        expect(filtered.observed).toBe(true);
        expect(harness.routeNativeElement?.querySelector('fd-product-list-groups')).toBeNull();
        expect(harness.routeNativeElement?.querySelector('fd-skeleton-card')).not.toBeNull();
        expect(registry.read()?.data).toMatchObject({ filters: { search: 'Pear' }, loading: true });
        await expect(screen.openEntityAsync(previousProduct.id)).rejects.toThrow('ID is not in the current product list');
        expect(getById).not.toHaveBeenCalled();

        filtered.next(productPage(currentProduct));
        filtered.complete();
        harness.detectChanges();
        expect(harness.routeNativeElement?.textContent).toContain(OWNING_RESULT);
        expect(harness.routeNativeElement?.textContent).not.toContain(PREVIOUS_RESULT);
        await expect(screen.openEntityAsync(previousProduct.id)).rejects.toThrow('ID is not in the current product list');
        await screen.openEntityAsync(currentProduct.id);
        harness.detectChanges();

        expect(router.url).toBe(`/products/${currentProduct.id}/edit`);
        expect(getById).toHaveBeenCalledExactlyOnceWith(currentProduct.id);
        expect(harness.routeNativeElement?.querySelector('fd-product-edit')).not.toBeNull();
        expect(registry.read()).toMatchObject({
            kind: 'product-editor',
            data: { isNew: false, id: currentProduct.id, values: { name: currentProduct.name } },
        });
    });
});
