import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { afterEach, beforeEach, describe, expect, it } from 'vitest';

import { environment } from '../../../../environments/environment';
import { ShoppingList, ShoppingListCreateDto, ShoppingListSummary, ShoppingListUpdateDto } from '../models/shopping-list.data';
import { ShoppingListService } from './shopping-list.service';

describe('ShoppingListService', () => {
    let service: ShoppingListService;
    let httpMock: HttpTestingController;

    const baseUrl = environment.apiUrls.shoppingLists;

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

    it('should be created', () => {
        expect(service).toBeTruthy();
    });

    it('should get current shopping list', () => {
        const mockList: ShoppingList = { id: '1', name: 'Current List', createdAt: '2026-01-01T00:00:00Z', items: [] };

        service.getCurrent().subscribe(result => {
            expect(result).toEqual(mockList);
        });

        const req = httpMock.expectOne(`${baseUrl}/current`);
        expect(req.request.method).toBe('GET');
        req.flush(mockList);
    });

    it('should get all shopping lists', () => {
        const mockLists: ShoppingListSummary[] = [
            { id: '1', name: 'List 1', createdAt: '2026-01-01T00:00:00Z', itemsCount: 0 },
            { id: '2', name: 'List 2', createdAt: '2026-01-02T00:00:00Z', itemsCount: 1 },
        ];

        service.getAll().subscribe(result => {
            expect(result).toEqual(mockLists);
        });

        const req = httpMock.expectOne(`${baseUrl}/`);
        expect(req.request.method).toBe('GET');
        req.flush(mockLists);
    });

    it('should get shopping list by id', () => {
        const mockList: ShoppingList = { id: 'abc-123', name: 'My List', createdAt: '2026-01-01T00:00:00Z', items: [] };

        service.getById('abc-123').subscribe(result => {
            expect(result).toEqual(mockList);
        });

        const req = httpMock.expectOne(`${baseUrl}/abc-123`);
        expect(req.request.method).toBe('GET');
        req.flush(mockList);
    });

    it('should create shopping list', () => {
        const createData: ShoppingListCreateDto = { name: 'New List' };
        const mockResponse: ShoppingList = { id: 'new-1', name: 'New List', createdAt: '2026-01-01T00:00:00Z', items: [] };

        service.create(createData).subscribe(result => {
            expect(result).toEqual(mockResponse);
        });

        const req = httpMock.expectOne(`${baseUrl}/`);
        expect(req.request.method).toBe('POST');
        expect(req.request.body).toEqual(createData);
        req.flush(mockResponse);
    });

    it('should update shopping list', () => {
        const updateData: ShoppingListUpdateDto = { name: 'Updated List' };
        const mockResponse: ShoppingList = { id: 'abc-123', name: 'Updated List', createdAt: '2026-01-01T00:00:00Z', items: [] };

        service.update('abc-123', updateData).subscribe(result => {
            expect(result).toEqual(mockResponse);
        });

        const req = httpMock.expectOne(`${baseUrl}/abc-123`);
        expect(req.request.method).toBe('PATCH');
        expect(req.request.body).toEqual(updateData);
        req.flush(mockResponse);
    });

    it('should delete shopping list', () => {
        service.deleteById('abc-123').subscribe();

        const req = httpMock.expectOne(`${baseUrl}/abc-123`);
        expect(req.request.method).toBe('DELETE');
        req.flush(null);
    });

    it('should return null on getCurrent failure', () => {
        service.getCurrent().subscribe(result => {
            expect(result).toBeNull();
        });

        const req = httpMock.expectOne(`${baseUrl}/current`);
        req.flush('Server Error', { status: 500, statusText: 'Internal Server Error' });
    });

    it('should return empty array on getAll failure', () => {
        service.getAll().subscribe(result => {
            expect(result).toEqual([]);
        });

        const req = httpMock.expectOne(`${baseUrl}/`);
        req.flush('Server Error', { status: 500, statusText: 'Internal Server Error' });
    });

    it('should return null on getById failure', () => {
        service.getById('abc-123').subscribe(result => {
            expect(result).toBeNull();
        });

        const req = httpMock.expectOne(`${baseUrl}/abc-123`);
        req.flush('Not Found', { status: 404, statusText: 'Not Found' });
    });
});
