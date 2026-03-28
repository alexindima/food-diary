import { TestBed } from '@angular/core/testing';
import { ActivatedRouteSnapshot, RouterStateSnapshot } from '@angular/router';
import { Observable, of, throwError } from 'rxjs';
import { productResolver } from './product.resolver';
import { ProductService } from '../api/product.service';
import { NavigationService } from '../../../services/navigation.service';
import { Product } from '../models/product.data';

describe('productResolver', () => {
    let productServiceSpy: { getById: ReturnType<typeof vi.fn> };
    let navSpy: { navigateToProductList: ReturnType<typeof vi.fn> };

    const mockProduct: Partial<Product> = { id: 'product-1' };

    const mockState = {} as unknown as RouterStateSnapshot;

    beforeEach(() => {
        productServiceSpy = { getById: vi.fn() };
        navSpy = { navigateToProductList: vi.fn().mockResolvedValue(undefined) };

        TestBed.configureTestingModule({
            providers: [
                { provide: ProductService, useValue: productServiceSpy },
                { provide: NavigationService, useValue: navSpy },
            ],
        });
    });

    it('should resolve product by id', () => {
        productServiceSpy.getById.mockReturnValue(of(mockProduct));

        let resolved: Product | null = null;

        const mockRoute = {
            paramMap: { get: vi.fn().mockReturnValue('product-1') },
        } as unknown as ActivatedRouteSnapshot;

        TestBed.runInInjectionContext(() => {
            const result$ = productResolver(mockRoute, mockState) as Observable<Product | null>;
            result$.subscribe(result => {
                resolved = result;
            });
        });

        expect(resolved).toEqual(mockProduct);
        expect(productServiceSpy.getById).toHaveBeenCalledWith('product-1');
    });

    it('should navigate to product list when id is missing', () => {
        const mockRoute = {
            paramMap: { get: vi.fn().mockReturnValue(null) },
        } as unknown as ActivatedRouteSnapshot;

        let resolved: Product | null | undefined;

        TestBed.runInInjectionContext(() => {
            const result$ = productResolver(mockRoute, mockState) as Observable<Product | null>;
            result$.subscribe(result => {
                resolved = result;
            });
        });

        expect(resolved).toBeNull();
        expect(navSpy.navigateToProductList).toHaveBeenCalled();
    });

    it('should navigate to product list when service throws error', () => {
        productServiceSpy.getById.mockReturnValue(throwError(() => new Error('not found')));

        const mockRoute = {
            paramMap: { get: vi.fn().mockReturnValue('product-1') },
        } as unknown as ActivatedRouteSnapshot;

        let resolved: Product | null | undefined;

        TestBed.runInInjectionContext(() => {
            const result$ = productResolver(mockRoute, mockState) as Observable<Product | null>;
            result$.subscribe(result => {
                resolved = result;
            });
        });

        expect(resolved).toBeNull();
        expect(navSpy.navigateToProductList).toHaveBeenCalled();
    });
});
