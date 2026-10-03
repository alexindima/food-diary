import { TestBed } from '@angular/core/testing';
import { of } from 'rxjs';
import { beforeEach, describe, expect, it, vi } from 'vitest';

import { type Product, ProductVisibility } from '../../../shared/models/product.data';
import { ProductService } from '../api/product.service';
import { ProductPublicationService } from './product-publication.service';

describe('ProductPublicationService', () => {
    const api = { getById: vi.fn(), update: vi.fn() };
    let service: ProductPublicationService;
    beforeEach(() => {
        vi.resetAllMocks();
        TestBed.configureTestingModule({ providers: [{ provide: ProductService, useValue: api }] });
        service = TestBed.inject(ProductPublicationService);
    });

    it('reads each product once and rejects missing products', async () => {
        api.getById.mockReturnValue(of(null));
        await expect(service.loadAsync(['one', 'one'])).rejects.toThrow('Cannot verify');
        expect(api.getById).toHaveBeenCalledTimes(1);
    });

    it('changes only visibility for owned products', async () => {
        api.getById.mockReturnValue(of({ id: 'one', isOwnedByCurrentUser: true }));
        api.update.mockReturnValue(of({}));
        const products = await service.loadAsync(['one']);
        await service.publishAsync(products);
        expect(api.update).toHaveBeenCalledWith('one', { visibility: ProductVisibility.Public });
    });

    it('does not publish foreign products', async () => {
        api.getById.mockReturnValue(of({ id: 'one', isOwnedByCurrentUser: false }));
        const products: Product[] = await service.loadAsync(['one']);
        await expect(service.publishAsync(products)).rejects.toThrow('another author');
        expect(api.update).not.toHaveBeenCalled();
    });
});
