import { HttpErrorResponse, HttpStatusCode } from '@angular/common/http';
import { RESPONSE_INIT } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { ActivatedRouteSnapshot, convertToParamMap, provideRouter, Router } from '@angular/router';
import { firstValueFrom, isObservable, of, throwError } from 'rxjs';
import { beforeEach, describe, expect, it, vi } from 'vitest';

import { PublicRecipeService } from '../api/public-recipe.service';
import { publicRecipeFixture } from '../lib/public-recipe.test-helper';
import { publicRecipeResolver } from './public-recipe.resolver';

describe('public recipe resolution', () => {
    const api = { getById: vi.fn() };
    let response: ResponseInit;
    beforeEach(() => {
        response = {};
        TestBed.configureTestingModule({
            providers: [provideRouter([]), { provide: PublicRecipeService, useValue: api }, { provide: RESPONSE_INIT, useValue: response }],
        });
    });
    async function resolveAsync(): Promise<unknown> {
        const route = new ActivatedRouteSnapshot();
        Object.defineProperty(route, 'paramMap', { value: convertToParamMap({ id: 'recipe' }) });
        const result = await TestBed.runInInjectionContext(async () =>
            publicRecipeResolver(route, TestBed.inject(Router).routerState.snapshot),
        );
        return isObservable(result) ? firstValueFrom(result) : result;
    }
    it('returns content and metadata for SSR without caching', async () => {
        api.getById.mockReturnValue(of(publicRecipeFixture()));
        expect(await resolveAsync()).toMatchObject({ title: 'Soup', error: null, recipe: { name: 'Soup' } });
        expect(new Headers(response.headers).get('Cache-Control')).toBe('no-store');
    });
    it.each([HttpStatusCode.NotFound, HttpStatusCode.BadRequest])('marks unavailable recipes as 404 and noindex (%i)', async status => {
        api.getById.mockReturnValue(throwError(() => new HttpErrorResponse({ status })));
        expect(await resolveAsync()).toMatchObject({ recipe: null, error: 'not-found', noIndex: true });
        expect(response.status).toBe(HttpStatusCode.NotFound);
    });
    it('distinguishes temporary failures from missing recipes', async () => {
        api.getById.mockReturnValue(throwError(() => new HttpErrorResponse({ status: 503 })));
        expect(await resolveAsync()).toMatchObject({ error: 'error', noIndex: true });
        expect(response.status).toBe(HttpStatusCode.ServiceUnavailable);
    });
});
