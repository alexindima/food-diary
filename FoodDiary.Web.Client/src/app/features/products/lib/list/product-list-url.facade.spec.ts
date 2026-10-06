import { signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { convertToParamMap } from '@angular/router';
import { TranslateService } from '@ngx-translate/core';
import { FdUiDialogService } from 'fd-ui-kit/dialog/fd-ui-dialog.service';
import { FdUiToastService } from 'fd-ui-kit/toast/fd-ui-toast.service';
import { BehaviorSubject, of, Subject } from 'rxjs';
import { describe, expect, it, vi } from 'vitest';

import { APP_SEARCH_DEBOUNCE_MS } from '../../../../config/runtime-ui.tokens';
import { NavigationService } from '../../../../services/navigation.service';
import type { PageOf } from '../../../../shared/models/page-of.data';
import type { Product, ProductOverview } from '../../../../shared/models/product.data';
import { ViewportService } from '../../../../shared/platform/viewport.service';
import { QuickMealService } from '../../../meals/contracts/quick-meal';
import { FavoriteProductService } from '../../api/favorite-product.service';
import { OpenFoodFactsService } from '../../api/open-food-facts.service';
import { ProductService } from '../../api/product.service';
import { PRODUCT_LIST_PAGE_SIZE } from '../../components/list/product-list.config';
import { ProductListFacade } from './product-list.facade';
import { type ProductListQuery, readProductListQuery } from './product-list-query';
import { PRODUCT_LIST_QUERY_STATE } from './product-list-query-state';

const DEFAULT_TOTAL_PAGES = 3;

describe('Product list page recovery', () => {
    it('replaces a stale page with the last result page and preserves ownership and filters', () => {
        const { facade, changes, products, writeAsync } = setup({ page: '3', onlyMine: 'true', types: 'Fruit', hasImage: 'false' }, true, {
            totalPages: 2,
        });
        expect(writeAsync).toHaveBeenCalledWith(
            expect.objectContaining({ page: 2, onlyMine: true, productTypes: ['Fruit'], hasImage: false }),
            { replaceUrl: true },
        );
        changes.next(readProductListQuery(convertToParamMap({ page: '2', onlyMine: 'true', types: 'Fruit', hasImage: 'false' })));
        expect(products.query).toHaveBeenLastCalledWith(
            2,
            PRODUCT_LIST_PAGE_SIZE,
            expect.objectContaining({ productTypes: ['Fruit'], hasImage: false }),
            false,
        );
        expect(facade.currentPageIndex).toBe(1);
        expect(facade.isEmptyState()).toBe(false);
    });

    it('corrects a true empty result to page one without a navigation loop', () => {
        const { facade, changes, writeAsync } = setup({ page: '3' }, true, { totalPages: 0 });
        expect(writeAsync).toHaveBeenCalledWith(expect.objectContaining({ page: 1 }), { replaceUrl: true });
        changes.next(readProductListQuery(convertToParamMap({})));
        expect(writeAsync).toHaveBeenCalledTimes(1);
        expect(facade.currentPageIndex).toBe(0);
        expect(facade.isEmptyState()).toBe(true);
    });

    it('does not let stale page recovery discard text typed before search debounce', () => {
        const pendingOverview = new Subject<ProductOverview>();
        const { facade, writeAsync } = setup({ page: '3' }, true, { totalPages: 2, pendingOverview });
        facade.searchForm.search().value.set('new draft');
        pendingOverview.next({
            allProducts: { data: [], page: 3, limit: PRODUCT_LIST_PAGE_SIZE, totalItems: 14, totalPages: 2 },
            recentItems: [],
            favoriteItems: [],
            favoriteTotalCount: 0,
        });
        expect(writeAsync).not.toHaveBeenCalled();
        expect(facade.searchValue()).toBe('new draft');
        expect(facade.productData.items()).toEqual([]);
    });
});

describe('Product list URL ownership', () => {
    it('restores filters and page from the URL in its initial overview', () => {
        const { facade, products } = setup({ search: 'tea', onlyMine: 'true', types: 'Fruit', page: '3' });
        expect(facade.searchValue()).toBe('tea');
        expect(facade.currentPageIndex).toBe(2);
        expect(products.queryOverview).toHaveBeenCalledWith(
            expect.objectContaining({
                page: 3,
                includePublic: false,
            }),
        );
        expect(products.queryOverview.mock.calls[0][0].filters).toEqual(
            expect.objectContaining({ search: 'tea', productTypes: ['Fruit'] }),
        );
        expect(products.query).not.toHaveBeenCalled();
    });

    it('loads a restored history entry once without writing a new URL or resetting its page', async () => {
        const { facade, products, changes, writeAsync } = setup({ search: 'tea', page: '3' });
        TestBed.tick();
        changes.next(readProductListQuery(convertToParamMap({ search: 'coffee', onlyMine: 'true', page: '2' })));
        TestBed.tick();
        await vi.waitFor(() => {
            expect(products.query).toHaveBeenCalledTimes(1);
        });
        expect(facade.searchValue()).toBe('coffee');
        expect(facade.currentPageIndex).toBe(1);
        expect(products.query).toHaveBeenCalledWith(2, expect.any(Number), expect.objectContaining({ search: 'coffee' }), false);
        expect(writeAsync).not.toHaveBeenCalled();
    });

    it('writes pagination to the URL and waits for the router to commit it', () => {
        const { facade, products, changes, writeAsync } = setup({ search: 'tea' });
        facade.onPageChange(2);
        expect(writeAsync).toHaveBeenCalledWith(expect.objectContaining({ search: 'tea', page: 3 }));
        expect(products.query).not.toHaveBeenCalled();
        changes.next(readProductListQuery(convertToParamMap({ search: 'tea', page: '3' })));
        expect(products.query).toHaveBeenCalledTimes(1);
    });

    it('resets the URL page when the user searches, without making a duplicate request', async () => {
        const { facade, products, changes, writeAsync } = setup({ page: '3' });
        TestBed.tick();
        facade.searchForm.search().value.set('tea');
        TestBed.tick();
        await vi.waitFor(() => {
            expect(writeAsync).toHaveBeenCalledWith(expect.objectContaining({ search: 'tea', page: 1 }));
        });
        expect(products.query).not.toHaveBeenCalled();
        changes.next(readProductListQuery(convertToParamMap({ search: 'tea' })));
        TestBed.tick();
        await vi.waitFor(() => {
            expect(products.query).toHaveBeenCalledTimes(1);
        });
    });

    it('keeps dialog filters local when no page capability is provided', async () => {
        const { facade, products, writeAsync } = setup({}, false);
        TestBed.tick();
        facade.searchForm.search().value.set('tea');
        TestBed.tick();
        await vi.waitFor(() => {
            expect(products.query).toHaveBeenCalledTimes(1);
        });
        expect(writeAsync).not.toHaveBeenCalled();
    });

    it('cancels the pending product read when its owner is destroyed', () => {
        const { facade, products } = setup({}, false);
        const pending = new Subject<PageOf<Product>>();
        products.query.mockReturnValue(pending);
        facade.loadProducts(1, facade.pageSize, 'tea').subscribe();
        expect(pending.observed).toBe(true);
        TestBed.resetTestingModule();
        expect(pending.observed).toBe(false);
    });
});

function setup(
    params: Record<string, string>,
    page = true,
    options: { totalPages?: number; pendingOverview?: ReturnType<ProductService['queryOverview']> } = {},
): {
    facade: ProductListFacade;
    products: {
        query: ReturnType<typeof vi.fn<ProductService['query']>>;
        queryOverview: ReturnType<typeof vi.fn<ProductService['queryOverview']>>;
    };
    changes: BehaviorSubject<ProductListQuery>;
    writeAsync: ReturnType<typeof vi.fn>;
} {
    const initial = readProductListQuery(convertToParamMap(params));
    const changes = new BehaviorSubject(initial);
    const writeAsync = vi.fn().mockResolvedValue(true);
    const totalPages = options.totalPages ?? DEFAULT_TOTAL_PAGES;
    const products = {
        query: vi.fn<ProductService['query']>((number: number) =>
            of({ data: [], page: number, limit: PRODUCT_LIST_PAGE_SIZE, totalPages, totalItems: totalPages * PRODUCT_LIST_PAGE_SIZE }),
        ),
        queryOverview: vi.fn<ProductService['queryOverview']>(
            query =>
                options.pendingOverview ??
                of({
                    allProducts: {
                        data: [],
                        page: query.page,
                        limit: PRODUCT_LIST_PAGE_SIZE,
                        totalPages,
                        totalItems: totalPages * PRODUCT_LIST_PAGE_SIZE,
                    },
                    recentItems: [],
                    favoriteItems: [],
                    favoriteTotalCount: 0,
                }),
        ),
    };
    TestBed.configureTestingModule({
        providers: [
            ProductListFacade,
            { provide: ProductService, useValue: products },
            { provide: FavoriteProductService, useValue: {} },
            { provide: OpenFoodFactsService, useValue: { search: vi.fn(() => of([])) } },
            { provide: QuickMealService, useValue: {} },
            { provide: NavigationService, useValue: {} },
            { provide: ViewportService, useValue: { isMobile: signal(false) } },
            { provide: FdUiDialogService, useValue: {} },
            { provide: FdUiToastService, useValue: {} },
            { provide: TranslateService, useValue: {} },
            { provide: APP_SEARCH_DEBOUNCE_MS, useValue: 0 },
            ...(page ? [{ provide: PRODUCT_LIST_QUERY_STATE, useValue: { initial, changes, writeAsync } }] : []),
        ],
    });
    return { facade: TestBed.inject(ProductListFacade), products, changes, writeAsync };
}
