import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';

import { environment } from '../../../../environments/environment';
import { PublicRecipeService } from './public-recipe.service';

describe('PublicRecipeService', () => {
    let service: PublicRecipeService;
    let http: HttpTestingController;
    beforeEach(() => {
        TestBed.configureTestingModule({ providers: [provideHttpClient(), provideHttpClientTesting()] });
        service = TestBed.inject(PublicRecipeService);
        http = TestBed.inject(HttpTestingController);
    });
    afterEach(() => {
        http.verify();
    });
    it('requests an explicitly public paginated catalog', () => {
        service.query({ page: 2, search: 'Soup', category: 'Dinner', maxTotalTime: 30, sortBy: 'fastest' }).subscribe();
        const request = http.expectOne(req => req.url === `${environment.apiUrls.recipes}/public/`);
        expect(request.request.params.get('limit')).toBe('20');
        expect(request.request.params.get('page')).toBe('2');
        expect(request.request.params.get('sortBy')).toBe('fastest');
        expect(request.request.params.get('maxTotalTime')).toBe('30');
        expect(request.request.params.has('includePublic')).toBe(false);
        request.flush({ data: [], totalItems: 0, totalPages: 0, page: 2, limit: 20 });
    });
    it('propagates not-found instead of pretending a private recipe is public', () => {
        const error = vi.fn();
        service.getById('private').subscribe({ error });
        http.expectOne(`${environment.apiUrls.recipes}/public/private`).flush({}, { status: 404, statusText: 'Not Found' });
        expect(error).toHaveBeenCalled();
    });
});
