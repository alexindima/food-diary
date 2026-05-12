import { HttpStatusCode, provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { afterEach, beforeEach, describe, expect, it } from 'vitest';

import { environment } from '../../../../environments/environment';
import type { ShoppingList, ShoppingListCreateDto, ShoppingListSummary, ShoppingListUpdateDto } from '../models/shopping-list.data';
import { ShoppingListService } from './shopping-list.service';

const BASE_URL = environment.apiUrls.shoppingLists;
const MOCK_LIST: ShoppingList = { id: 'abc-123', name: 'My List', createdAt: '2026-01-01T00:00:00Z', items: [] };
const MOCK_SUMMARIES: ShoppingListSummary[] = [
    { id: '1', name: 'List 1', createdAt: '2026-01-01T00:00:00Z', itemsCount: 0 },
    { id: '2', name: 'List 2', createdAt: '2026-01-02T00:00:00Z', itemsCount: 1 },
];

let service: ShoppingListService;
let httpMock: HttpTestingController;

beforeEach(() => {
    TestBed.configureTestingModule({
        providers: [ShoppingListService, provideHttpClient(), provideHttpClientTesting()],
    });

    service = TestBed.inject(ShoppingListService);
    httpMock = TestBed.inject(HttpTestingController);
});

afterEach(() => {
    httpMock.verify();
});

describe('ShoppingListService', () => {
    it('should be created', () => {
        expect(service).toBeTruthy();
    });
});

describe('ShoppingListService reads', () => {
    it('should get current shopping list', () => {
        const currentList: ShoppingList = { ...MOCK_LIST, id: '1', name: 'Current List' };

        service.getCurrent().subscribe(result => {
            expect(result).toEqual(currentList);
        });

        const req = httpMock.expectOne(`${BASE_URL}/current`);
        expect(req.request.method).toBe('GET');
        req.flush(currentList);
    });

    it('should get all shopping lists', () => {
        service.getAll().subscribe(result => {
            expect(result).toEqual(MOCK_SUMMARIES);
        });

        const req = httpMock.expectOne(`${BASE_URL}/`);
        expect(req.request.method).toBe('GET');
        req.flush(MOCK_SUMMARIES);
    });

    it('should get shopping list by id', () => {
        service.getById('abc-123').subscribe(result => {
            expect(result).toEqual(MOCK_LIST);
        });

        const req = httpMock.expectOne(`${BASE_URL}/abc-123`);
        expect(req.request.method).toBe('GET');
        req.flush(MOCK_LIST);
    });
});

describe('ShoppingListService mutations', () => {
    it('should create shopping list', () => {
        const createData: ShoppingListCreateDto = { name: 'New List' };
        const response: ShoppingList = { ...MOCK_LIST, id: 'new-1', name: 'New List' };

        service.create(createData).subscribe(result => {
            expect(result).toEqual(response);
        });

        const req = httpMock.expectOne(`${BASE_URL}/`);
        expect(req.request.method).toBe('POST');
        expect(req.request.body).toEqual(createData);
        req.flush(response);
    });

    it('should update shopping list', () => {
        const updateData: ShoppingListUpdateDto = { name: 'Updated List' };
        const response: ShoppingList = { ...MOCK_LIST, name: 'Updated List' };

        service.update('abc-123', updateData).subscribe(result => {
            expect(result).toEqual(response);
        });

        const req = httpMock.expectOne(`${BASE_URL}/abc-123`);
        expect(req.request.method).toBe('PATCH');
        expect(req.request.body).toEqual(updateData);
        req.flush(response);
    });

    it('should delete shopping list', () => {
        service.deleteById('abc-123').subscribe();

        const req = httpMock.expectOne(`${BASE_URL}/abc-123`);
        expect(req.request.method).toBe('DELETE');
        req.flush(null);
    });
});

describe('ShoppingListService failures', () => {
    it('should return null on getCurrent failure', () => {
        service.getCurrent().subscribe(result => {
            expect(result).toBeNull();
        });

        const req = httpMock.expectOne(`${BASE_URL}/current`);
        req.flush('Server Error', { status: HttpStatusCode.InternalServerError, statusText: 'Internal Server Error' });
    });

    it('should return empty array on getAll failure', () => {
        service.getAll().subscribe(result => {
            expect(result).toEqual([]);
        });

        const req = httpMock.expectOne(`${BASE_URL}/`);
        req.flush('Server Error', { status: HttpStatusCode.InternalServerError, statusText: 'Internal Server Error' });
    });

    it('should return null on getById failure', () => {
        service.getById('abc-123').subscribe(result => {
            expect(result).toBeNull();
        });

        const req = httpMock.expectOne(`${BASE_URL}/abc-123`);
        req.flush('Not Found', { status: 404, statusText: 'Not Found' });
    });
});
