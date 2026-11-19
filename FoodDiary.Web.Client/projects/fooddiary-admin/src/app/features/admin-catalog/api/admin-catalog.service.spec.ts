import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { retry } from 'rxjs';
import { afterEach, beforeEach, describe, expect, it } from 'vitest';

import type { CatalogProduct, CatalogRecipe, CatalogResult } from '../models/catalog-transfer';
import { AdminCatalogService } from './admin-catalog.service';

const product: CatalogProduct = {
    id: 'product',
    name: 'Product',
    barcode: null,
    brand: null,
    productType: 'Food',
    category: null,
    description: null,
    imageUrl: null,
    baseUnit: 'g',
    baseAmount: 100,
    defaultPortionAmount: 0,
    caloriesPerBase: 0,
    proteinsPerBase: 1.23456789,
    fatsPerBase: 0,
    carbsPerBase: 0,
    fiberPerBase: 0,
    alcoholPerBase: 0,
};
const recipe: CatalogRecipe = {
    id: 'recipe',
    name: 'Recipe',
    description: null,
    category: null,
    imageUrl: null,
    prepTime: null,
    cookTime: null,
    servings: 2,
    language: 'en',
    languageConfirmed: true,
    calculateNutritionAutomatically: false,
    manualCalories: 0,
    manualProteins: null,
    manualFats: null,
    manualCarbs: null,
    manualFiber: null,
    manualAlcohol: null,
    steps: [
        {
            order: 1,
            description: 'Combine',
            title: null,
            imageUrl: null,
            ingredients: [{ amount: 0.125, productId: null, nestedRecipeId: 'sauce' }],
        },
    ],
};
let http: HttpTestingController;
let service: AdminCatalogService;

beforeEach(() => {
    TestBed.configureTestingModule({ providers: [provideHttpClient(), provideHttpClientTesting()] });
    http = TestBed.inject(HttpTestingController);
    service = TestBed.inject(AdminCatalogService);
});
afterEach(() => {
    http.verify();
});

describe('AdminCatalogService generated requests', () => {
    it('exports products and recipes together after both requests complete', () => {
        const results: Array<{ products: CatalogProduct[]; recipes: CatalogRecipe[] }> = [];
        service.exportCatalog().subscribe(value => results.push(value));

        const products = http.expectOne(request => request.method === 'GET' && request.url.endsWith('/admin/catalog/products/export'));
        const recipes = http.expectOne(request => request.method === 'GET' && request.url.endsWith('/admin/catalog/recipes/export'));
        recipes.flush([recipe]);
        expect(results).toEqual([]);

        products.flush([{ ...product, barcode: undefined }]);
        expect(results).toEqual([{ products: [product], recipes: [recipe] }]);
    });

    it('previews recipes and products on their own endpoints and preserves errors per item', () => {
        const results: CatalogResult[] = [];
        service.preview('products', product).subscribe(value => results.push(value));
        service.preview('recipes', recipe).subscribe(value => results.push(value));

        const productPreview = http.expectOne(
            request => request.method === 'POST' && request.url.endsWith('/admin/catalog/products/preview'),
        );
        expect(productPreview.request.body).toEqual(product);
        const productResult = { id: product.id, status: 'ready', errors: [] };
        productPreview.flush(productResult);

        const recipePreview = http.expectOne(
            request => request.method === 'POST' && request.url.endsWith('/admin/catalog/recipes/preview'),
        );
        expect(recipePreview.request.body).toEqual(recipe);
        const recipeResult = { id: recipe.id, status: 'invalid', errors: ['Nested recipe does not exist'] };
        recipePreview.flush(recipeResult);

        expect(results).toEqual([productResult, recipeResult]);
    });

    it('retains the import idempotency key and original nutrient payload across retry', () => {
        let result: CatalogResult | undefined;
        service
            .importItem('products', product)
            .pipe(retry(1))
            .subscribe(value => {
                result = value;
            });
        const first = http.expectOne(request => request.url.endsWith('/admin/catalog/products/import'));
        const key = first.request.headers.get('Idempotency-Key');
        expect(key).toBeTruthy();
        expect(first.request.body).toEqual(product);
        first.flush('temporary', { status: 503, statusText: 'Unavailable' });
        const second = http.expectOne(request => request.url.endsWith('/admin/catalog/products/import'));
        expect(second.request.headers.get('Idempotency-Key')).toBe(key);
        second.flush({ id: 'product', status: 'imported', errors: [] });
        expect(result?.status).toBe('imported');
    });
});
