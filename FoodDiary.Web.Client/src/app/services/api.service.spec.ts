import { HttpContext, HttpHeaders, HttpResponse, provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { Injectable } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import type { Observable } from 'rxjs';
import { describe, expect, it } from 'vitest';

import { ApiService } from './api.service';

const BASE_URL = 'http://api.test';
const ENDPOINT = 'items';

@Injectable()
class TestApiService extends ApiService {
    protected readonly baseUrl = BASE_URL;

    public read(): Observable<{ ok: boolean }> {
        return this.get<{ ok: boolean }>(ENDPOINT, { page: 1, search: 'apple', skip: null, empty: undefined });
    }

    public create(): Observable<{ id: string }> {
        return this.post<{ id: string }>(ENDPOINT, { name: 'Apple' }, new HttpHeaders({ 'X-Test': 'yes' }), { source: 'manual' });
    }

    public replace(): Observable<{ id: string }> {
        return this.put<{ id: string }>(ENDPOINT, { name: 'Pear' });
    }

    public update(): Observable<{ id: string }> {
        return this.patch<{ id: string }>(ENDPOINT, { name: 'Banana' });
    }

    public remove(): Observable<void> {
        return this.delete<void>(ENDPOINT);
    }

    public readBlob(): Observable<HttpResponse<Blob>> {
        return this.getBlob(ENDPOINT, { export: true });
    }

    public readWithContext(context: HttpContext): Observable<{ ok: boolean }> {
        return this.get<{ ok: boolean }>(ENDPOINT, undefined, undefined, context);
    }
}

describe('ApiService', () => {
    it('builds GET urls and skips nullish params', () => {
        const { service, httpMock } = setup();

        service.read().subscribe(result => {
            expect(result).toEqual({ ok: true });
        });

        const req = httpMock.expectOne(request => request.url === `${BASE_URL}/${ENDPOINT}`);
        expect(req.request.method).toBe('GET');
        expect(req.request.params.get('page')).toBe('1');
        expect(req.request.params.get('search')).toBe('apple');
        expect(req.request.params.has('skip')).toBe(false);
        expect(req.request.params.has('empty')).toBe(false);
        req.flush({ ok: true });
        httpMock.verify();
    });

    it('passes bodies, headers, params, and contexts to HTTP requests', () => {
        const { service, httpMock } = setup();
        const context = new HttpContext();

        service.create().subscribe();
        const post = httpMock.expectOne(`${BASE_URL}/${ENDPOINT}?source=manual`);
        expect(post.request.method).toBe('POST');
        expect(post.request.body).toEqual({ name: 'Apple' });
        expect(post.request.headers.get('X-Test')).toBe('yes');
        post.flush({ id: '1' });

        service.replace().subscribe();
        const put = httpMock.expectOne(`${BASE_URL}/${ENDPOINT}`);
        expect(put.request.method).toBe('PUT');
        expect(put.request.body).toEqual({ name: 'Pear' });
        put.flush({ id: '1' });

        service.update().subscribe();
        const patch = httpMock.expectOne(`${BASE_URL}/${ENDPOINT}`);
        expect(patch.request.method).toBe('PATCH');
        expect(patch.request.body).toEqual({ name: 'Banana' });
        patch.flush({ id: '1' });

        service.remove().subscribe();
        const deleteRequest = httpMock.expectOne(`${BASE_URL}/${ENDPOINT}`);
        expect(deleteRequest.request.method).toBe('DELETE');
        deleteRequest.flush(null);

        service.readWithContext(context).subscribe();
        const contextual = httpMock.expectOne(`${BASE_URL}/${ENDPOINT}`);
        expect(contextual.request.context).toBe(context);
        contextual.flush({ ok: true });

        httpMock.verify();
    });

    it('reads blob responses with response metadata', () => {
        const { service, httpMock } = setup();
        const body = new Blob(['data']);

        service.readBlob().subscribe(response => {
            expect(response).toBeInstanceOf(HttpResponse);
            expect(response.body).toBe(body);
        });

        const req = httpMock.expectOne(`${BASE_URL}/${ENDPOINT}?export=true`);
        expect(req.request.method).toBe('GET');
        expect(req.request.responseType).toBe('blob');
        req.flush(body);
        httpMock.verify();
    });
});

function setup(): { service: TestApiService; httpMock: HttpTestingController } {
    TestBed.configureTestingModule({
        providers: [TestApiService, provideHttpClient(), provideHttpClientTesting()],
    });

    return {
        service: TestBed.inject(TestApiService),
        httpMock: TestBed.inject(HttpTestingController),
    };
}
