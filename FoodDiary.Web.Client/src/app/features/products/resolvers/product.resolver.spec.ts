import { TestBed } from '@angular/core/testing';
import type { ActivatedRouteSnapshot, RouterStateSnapshot } from '@angular/router';
import { type Observable, of, throwError } from 'rxjs';
import { beforeEach, describe, expect, it, vi } from 'vitest';

import { NavigationService } from '../../../services/navigation.service';
import { ProductService } from '../api/product.service';
import type { Product } from '../models/product.data';
import { productResolver } from './product.resolver';

let productServiceSpy: { getById: ReturnType<typeof vi.fn> };
let navSpy: { navigateToProductListAsync: ReturnType<typeof vi.fn> };

const mockProduct: Partial<Product> = { id: 'product-1', isOwnedByCurrentUser: true, usageCount: 0 };
const mockState = {} as unknown as RouterStateSnapshot;

describe('productResolver', () => {
    beforeEach(() => {
        productServiceSpy = { getById: vi.fn() };
        navSpy = { navigateToProductListAsync: vi.fn().mockResolvedValue(undefined) };

        TestBed.configureTestingModule({
            providers: [
                { provide: ProductService, useValue: productServiceSpy },
                { provide: NavigationService, useValue: navSpy },
            ],
        });
    });

    it('should resolve product by id', () => {
        productServiceSpy.getById.mockReturnValue(of(mockProduct));

        const resolved = resolveProductRoute('product-1');

        expect(resolved).toEqual(mockProduct);
        expect(productServiceSpy.getById).toHaveBeenCalledWith('product-1');
    });

    it('should navigate to product list when id is missing', () => {
        const resolved = resolveProductRoute(null);

        expect(resolved).toBeNull();
        expect(navSpy.navigateToProductListAsync).toHaveBeenCalled();
    });

    it('should navigate to product list when service throws error', () => {
        productServiceSpy.getById.mockReturnValue(throwError(() => new Error('not found')));

        const resolved = resolveProductRoute('product-1');

        expect(resolved).toBeNull();
        expect(navSpy.navigateToProductListAsync).toHaveBeenCalled();
    });

    it('should navigate to product list when product is already used', () => {
        productServiceSpy.getById.mockReturnValue(of({ ...mockProduct, usageCount: 1 }));

        const resolved = resolveProductRoute('product-1');

        expect(resolved).toBeNull();
        expect(navSpy.navigateToProductListAsync).toHaveBeenCalled();
    });

    it('should navigate to product list when product is not owned by current user', () => {
        productServiceSpy.getById.mockReturnValue(of({ ...mockProduct, isOwnedByCurrentUser: false }));

        const resolved = resolveProductRoute('product-1');

        expect(resolved).toBeNull();
        expect(navSpy.navigateToProductListAsync).toHaveBeenCalled();
    });
});

function resolveProductRoute(id: string | null): Product | null | undefined {
    let resolved: Product | null | undefined;
    TestBed.runInInjectionContext(() => {
        const result$ = productResolver(createRoute(id), mockState) as Observable<Product | null>;
        result$.subscribe(result => {
            resolved = result;
        });
    });
    return resolved;
}

function createRoute(id: string | null): ActivatedRouteSnapshot {
    return {
        paramMap: { get: vi.fn().mockReturnValue(id) },
    } as unknown as ActivatedRouteSnapshot;
}
