import { ChangeDetectionStrategy, Component, computed, inject, input, signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { convertToParamMap, provideRouter, Router, withComponentInputBinding } from '@angular/router';
import { RouterTestingHarness } from '@angular/router/testing';
import { beforeEach, describe, expect, it, vi } from 'vitest';

import { ProductType } from '../../../../shared/models/product.data';
import { type ProductListQuery, readProductListQuery } from './product-list-query';
import { ProductListRouteStateFacade } from './product-list-route-state.facade';

@Component({
    template: '',
    providers: [ProductListRouteStateFacade],
    changeDetection: ChangeDetectionStrategy.OnPush,
})
class ListRoute {
    public readonly search = input<string | string[]>();
    public readonly page = input<string | string[]>();
    public readonly onlyMine = input<string | string[]>();
    public readonly types = input<string | string[]>();
    public readonly state = inject(ProductListRouteStateFacade);

    public constructor() {
        this.state.bind(
            computed(() =>
                readProductListQuery(
                    convertToParamMap({
                        search: this.search() ?? null,
                        page: this.page() ?? null,
                        onlyMine: this.onlyMine() ?? null,
                        types: this.types() ?? [],
                    }),
                ),
            ),
        );
    }
}

const DEFAULT_QUERY: ProductListQuery = {
    search: null,
    page: 1,
    onlyMine: false,
    productTypes: [],
    caloriesFrom: null,
    caloriesTo: null,
    hasImage: null,
};

beforeEach(() => {
    TestBed.configureTestingModule({
        providers: [provideRouter([{ path: 'products', component: ListRoute }], withComponentInputBinding())],
    });
});

describe('Product list route query ownership', () => {
    it('uses defaults without rewriting a URL that has no product query', async () => {
        const harness = await RouterTestingHarness.create();
        const page = await harness.navigateByUrl('/products?campaign=qa', ListRoute);
        const navigate = vi.spyOn(TestBed.inject(Router), 'navigate');

        expect(page.state.initial).toEqual(DEFAULT_QUERY);
        expect(await page.state.normalizePageAsync(page.state.initial)).toBe(false);
        expect(await page.state.writeAsync(DEFAULT_QUERY)).toBe(false);
        expect(navigate).not.toHaveBeenCalled();
    });

    it.each(['0', 'abc'])('replaces invalid page=%s without dropping filters or unrelated query keys', async invalidPage => {
        const harness = await RouterTestingHarness.create();
        const page = await harness.navigateByUrl(
            `/products?page=${invalidPage}&search=tea&onlyMine=true&types=Fruit&hasImage=false&campaign=qa`,
            ListRoute,
        );
        const router = TestBed.inject(Router);
        const navigate = vi.spyOn(router, 'navigate');

        expect(page.state.initial).toEqual({
            ...DEFAULT_QUERY,
            search: 'tea',
            onlyMine: true,
            productTypes: [ProductType.Fruit],
            hasImage: false,
        });
        expect(await page.state.normalizePageAsync(page.state.initial)).toBe(true);
        expect(navigate).toHaveBeenCalledExactlyOnceWith([], expect.objectContaining({ queryParamsHandling: 'merge', replaceUrl: true }));
        expect(router.parseUrl(router.url).queryParams).toEqual({
            search: 'tea',
            onlyMine: 'true',
            types: 'Fruit',
            hasImage: 'false',
            campaign: 'qa',
        });

        expect(await page.state.normalizePageAsync(page.state.initial)).toBe(false);
        expect(navigate).toHaveBeenCalledOnce();
    });

    it('clears owned query keys when filters reset and retains unrelated keys', async () => {
        const harness = await RouterTestingHarness.create();
        const page = await harness.navigateByUrl(
            '/products?search=tea&page=2&onlyMine=true&types=Fruit&caloriesFrom=10&caloriesTo=200&hasImage=false&campaign=qa',
            ListRoute,
        );
        const router = TestBed.inject(Router);
        const navigate = vi.spyOn(router, 'navigate');

        expect(await page.state.writeAsync(DEFAULT_QUERY)).toBe(true);
        expect(navigate).toHaveBeenCalledExactlyOnceWith([], expect.objectContaining({ queryParamsHandling: 'merge', replaceUrl: false }));
        expect(router.parseUrl(router.url).queryParams).toEqual({ campaign: 'qa' });
        expect(await page.state.writeAsync(DEFAULT_QUERY)).toBe(false);
        expect(navigate).toHaveBeenCalledOnce();
    });
});

describe('Product list route query equivalence', () => {
    it('emits the initial URL query before route inputs bind', async () => {
        TestBed.configureTestingModule({ providers: [ProductListRouteStateFacade] });
        const harness = await RouterTestingHarness.create();
        await harness.navigateByUrl('/products?search=tea', ListRoute);
        const state = TestBed.inject(ProductListRouteStateFacade);
        const onChange = vi.fn();
        const subscription = state.changes.subscribe(onChange);

        TestBed.tick();
        expect(onChange).toHaveBeenCalledExactlyOnceWith({ ...DEFAULT_QUERY, search: 'tea' });

        const bound = { ...DEFAULT_QUERY, search: 'coffee' };
        state.bind(signal(bound));
        TestBed.tick();
        expect(onChange).toHaveBeenCalledTimes(2);
        expect(onChange).toHaveBeenLastCalledWith(bound);
        subscription.unsubscribe();
    });

    it('skips navigation when the requested query is canonically equivalent to the committed query', async () => {
        const harness = await RouterTestingHarness.create();
        const page = await harness.navigateByUrl('/products?search=tea&page=2&types=Fruit,Meat&campaign=qa', ListRoute);
        const navigate = vi.spyOn(TestBed.inject(Router), 'navigate');

        expect(
            await page.state.writeAsync({
                ...page.state.initial,
                search: ' tea ',
                productTypes: [ProductType.Meat, ProductType.Fruit, ProductType.Meat],
            }),
        ).toBe(false);
        expect(await page.state.normalizePageAsync(page.state.initial)).toBe(false);
        expect(navigate).not.toHaveBeenCalled();
    });

    it('emits committed product queries once while ignoring equivalent and unrelated URL changes', async () => {
        const harness = await RouterTestingHarness.create();
        const page = await harness.navigateByUrl('/products?search=tea&types=Fruit,Meat&campaign=first', ListRoute);
        const onChange = vi.fn();
        const subscription = page.state.changes.subscribe(onChange);

        expect(onChange).toHaveBeenCalledExactlyOnceWith(page.state.initial);
        await harness.navigateByUrl('/products?search=%20tea%20&types=Meat&types=Fruit,Fruit&campaign=second', ListRoute);
        expect(onChange).toHaveBeenCalledOnce();

        await harness.navigateByUrl(
            '/products?search=coffee&search=ignored&page=2&onlyMine=true&types=Fruit&types=Meat&campaign=second',
            ListRoute,
        );
        expect(onChange).toHaveBeenCalledTimes(2);
        expect(onChange).toHaveBeenLastCalledWith({
            ...DEFAULT_QUERY,
            search: 'coffee',
            page: 2,
            onlyMine: true,
            productTypes: [ProductType.Fruit, ProductType.Meat],
        });
        expect(page.state.initial.search).toBe('tea');

        onChange.mockClear();
        await harness.navigateByUrl('/products', ListRoute);
        expect(onChange).toHaveBeenCalledExactlyOnceWith(DEFAULT_QUERY);

        subscription.unsubscribe();
        await harness.navigateByUrl('/products?search=cocoa', ListRoute);
        expect(onChange).toHaveBeenCalledOnce();
    });
});
