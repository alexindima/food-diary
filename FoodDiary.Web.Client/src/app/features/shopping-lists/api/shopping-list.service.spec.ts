import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ShoppingListService } from './shopping-list.service';
import { environment } from '../../../../environments/environment';

describe('ShoppingListService', () => {
    let service: ShoppingListService;
    let httpMock: HttpTestingController;

    const baseUrl = environment.apiUrls.shoppingLists;

    beforeEach(() => {
        TestBed.configureTestingModule({
            providers: [
                ShoppingListService,
                provideHttpClient(),
                provideHttpClientTesting(),
            ],
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
        const mockList = { id: '1', name: 'Current List', items: [] };

        service.getCurrent().subscribe(result => {
            expect(result).toEqual(mockList as any);
        });

        const req = httpMock.expectOne(`${baseUrl}/current`);
        expect(req.request.method).toBe('GET');
        req.flush(mockList);
    });

    it('should get all shopping lists', () => {
        const mockLists = [
            { id: '1', name: 'List 1' },
            { id: '2', name: 'List 2' },
        ];

        service.getAll().subscribe(result => {
            expect(result).toEqual(mockLists as any);
        });

        const req = httpMock.expectOne(`${baseUrl}/`);
        expect(req.request.method).toBe('GET');
        req.flush(mockLists);
    });

    it('should get shopping list by id', () => {
        const mockList = { id: 'abc-123', name: 'My List', items: [] };

        service.getById('abc-123').subscribe(result => {
            expect(result).toEqual(mockList as any);
        });

        const req = httpMock.expectOne(`${baseUrl}/abc-123`);
        expect(req.request.method).toBe('GET');
        req.flush(mockList);
    });

    it('should create shopping list', () => {
        const createData = { name: 'New List' };
        const mockResponse = { id: 'new-1', name: 'New List', items: [] };

        service.create(createData as any).subscribe(result => {
            expect(result).toEqual(mockResponse as any);
        });

        const req = httpMock.expectOne(`${baseUrl}/`);
        expect(req.request.method).toBe('POST');
        expect(req.request.body).toEqual(createData);
        req.flush(mockResponse);
    });

    it('should update shopping list', () => {
        const updateData = { name: 'Updated List' };
        const mockResponse = { id: 'abc-123', name: 'Updated List', items: [] };

        service.update('abc-123', updateData as any).subscribe(result => {
            expect(result).toEqual(mockResponse as any);
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
