import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { provideRouter, Router } from '@angular/router';
import { RouterTestingHarness } from '@angular/router/testing';
import { beforeEach, describe, expect, it, vi } from 'vitest';

import { ProductType } from '../../../../shared/models/product.data';
import type { ProductListQuery } from './product-list-query';
import { ProductListRouteStateFacade } from './product-list-route-state.facade';

@Component({
    template: '',
    providers: [ProductListRouteStateFacade],
    changeDetection: ChangeDetectionStrategy.OnPush,
})
class ListRoute {
    public readonly state = inject(ProductListRouteStateFacade);
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
    TestBed.configureTestingModule({ providers: [provideRouter([{ path: 'products', component: ListRoute }])] });
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
        await harness.navigateByUrl('/products?search=%20tea%20&types=Meat,Fruit,Fruit&campaign=second', ListRoute);
        expect(onChange).toHaveBeenCalledOnce();

        await harness.navigateByUrl('/products?search=coffee&page=2&onlyMine=true&types=Fruit,Meat&campaign=second', ListRoute);
        expect(onChange).toHaveBeenCalledTimes(2);
        expect(onChange).toHaveBeenLastCalledWith({
            ...DEFAULT_QUERY,
            search: 'coffee',
            page: 2,
            onlyMine: true,
            productTypes: [ProductType.Fruit, ProductType.Meat],
        });
        expect(page.state.initial.search).toBe('tea');

        subscription.unsubscribe();
        await harness.navigateByUrl('/products', ListRoute);
        expect(onChange).toHaveBeenCalledTimes(2);
    });
});
