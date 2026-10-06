import { HttpClient, type HttpInterceptorFn, provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { firstValueFrom } from 'rxjs';
import { afterEach, describe, expect, it } from 'vitest';

import { createProductsSdk } from './products-sdk';

afterEach(() => {
    TestBed.inject(HttpTestingController).verify();
});

function connect(baseUrl: string): ReturnType<typeof createProductsSdk> {
    TestBed.configureTestingModule({
        providers: [
            provideHttpClient(
                withInterceptors([
                    (request, next): ReturnType<HttpInterceptorFn> => next(request.clone({ setHeaders: { 'X-Test-Pipeline': 'active' } })),
                ]),
            ),
            provideHttpClientTesting(),
        ],
    });
    return createProductsSdk(baseUrl, TestBed.inject(HttpClient));
}

describe('Products SDK HTTP integration', () => {
    it.each(['https://api.example.com', '', '/gateway'])(
        'uses the configured API base %s, cookies and Angular interceptors',
        async base => {
            const sdk = connect(`${base}/api/v1/products`);
            const result = firstValueFrom(sdk.client.getProductSuggestions({ version: sdk.version, search: 'milk & rice', limit: 3 }));
            const request = TestBed.inject(HttpTestingController).expectOne(item => item.url === `${base}/api/v1/products/suggestions`);
            expect(new URL(request.request.urlWithParams, 'https://test.example.com').searchParams.get('search')).toBe('milk & rice');
            expect(request.request.params.get('limit')).toBe('3');
            expect(request.request.withCredentials).toBe(true);
            expect(request.request.headers.get('X-Test-Pipeline')).toBe('active');
            expect(request.request.headers.get('Accept')).toBe('application/json');
            request.flush([{ source: 'usda', name: 'Rice', barcode: null }]);
            expect(await result).toEqual([{ source: 'usda', name: 'Rice', barcode: null }]);
        },
    );

    it('uses the configured version and encodes identifiers in mutation paths', async () => {
        const sdk = connect('/api/v2/products/');
        const result = firstValueFrom(sdk.client.deleteProduct({ version: sdk.version, id: 'id /?' }));
        const request = TestBed.inject(HttpTestingController).expectOne('/api/v2/products/id%20%2F%3F');
        expect(request.request.method).toBe('DELETE');
        expect(request.request.withCredentials).toBe(true);
        expect(request.request.headers.get('X-Test-Pipeline')).toBe('active');
        request.flush(null, { status: 204, statusText: 'No Content' });
        await result;
    });
});
