import { HttpErrorResponse, HttpStatusCode } from '@angular/common/http';
import { Component, RESPONSE_INIT } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { provideRouter, Router } from '@angular/router';
import { RouterTestingHarness } from '@angular/router/testing';
import { of, throwError } from 'rxjs';
import { beforeEach, describe, expect, it, vi } from 'vitest';

import { PublicRecipeService } from '../api/public-recipe.service';
import { publicRecipeFixture } from '../lib/public-recipe.test-helper';
import { publicRecipeResolver } from './public-recipe.resolver';

@Component({ template: '' })
class RecipeRouteOutlet {}

describe('public recipe resolution', () => {
    const api = { getById: vi.fn() };
    let response: ResponseInit;
    beforeEach(() => {
        response = {};
        TestBed.configureTestingModule({
            providers: [
                provideRouter([{ path: ':id', component: RecipeRouteOutlet, resolve: { recipe: publicRecipeResolver } }]),
                { provide: PublicRecipeService, useValue: api },
                { provide: RESPONSE_INIT, useValue: response },
            ],
        });
    });
    async function resolveAsync(): Promise<unknown> {
        await RouterTestingHarness.create('/recipe');
        return TestBed.inject(Router).routerState.snapshot.root.firstChild?.data['recipe'];
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
