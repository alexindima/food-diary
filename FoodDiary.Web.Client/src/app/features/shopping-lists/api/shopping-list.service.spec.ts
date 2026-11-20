import { HttpStatusCode, provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import type { Observable } from 'rxjs';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';

import { environment } from '../../../../environments/environment';
import type { ShoppingListOverviewHttpResponse } from '../../../shared/api/sdk/generated/model/shopping-list-overview-http-response';
import { utcInstant } from '../../../shared/models/semantics/date-value';
import { entityId } from '../../../shared/models/semantics/entity-id';
import type {
    ShoppingList,
    ShoppingListCreateDto,
    ShoppingListSummary,
    ShoppingListUpdateDto,
} from '../../../shared/models/shopping-list.data';
import { ShoppingListService } from './shopping-list.service';

const BASE_URL = environment.apiUrls.shoppingLists;
const REVISION_LENGTH = 64;
const DESTINATION_PAGE_SIZE = 50;
const MOCK_LIST: ShoppingList = {
    id: entityId<'shopping-list'>('abc-123'),
    revision: 'a'.repeat(REVISION_LENGTH),
    name: 'My List',
    createdAt: utcInstant('2026-01-01T00:00:00Z'),
    items: [],
};
const MOCK_SUMMARIES: ShoppingListSummary[] = [
    { id: entityId<'shopping-list'>('1'), name: 'List 1', createdAt: utcInstant('2026-01-01T00:00:00Z'), itemsCount: 0 },
    { id: entityId<'shopping-list'>('2'), name: 'List 2', createdAt: utcInstant('2026-01-02T00:00:00Z'), itemsCount: 1 },
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
        const currentList: ShoppingList = { ...MOCK_LIST, id: entityId<'shopping-list'>('1'), name: 'Current List' };

        service.getCurrent().subscribe(result => {
            expect(result).toEqual(currentList);
        });

        const req = httpMock.expectOne(`${BASE_URL}/current`);
        expect(req.request.method).toBe('GET');
        req.flush(currentList);
    });

    it('should get first page of shopping lists', () => {
        service.getPage().subscribe(result => {
            expect(result).toEqual(MOCK_SUMMARIES);
        });

        const req = httpMock.expectOne(`${BASE_URL}/page?page=1&limit=20&search=`);
        expect(req.request.method).toBe('GET');
        req.flush(MOCK_SUMMARIES);
    });

    it('should get shopping list by id', () => {
        service.getById(entityId<'shopping-list'>('abc-123')).subscribe(result => {
            expect(result).toEqual(MOCK_LIST);
        });

        const req = httpMock.expectOne(`${BASE_URL}/abc-123`);
        expect(req.request.method).toBe('GET');
        req.flush(MOCK_LIST);
    });
});

describe('ShoppingListService picker and overview contracts', () => {
    it('loads all destination pages, keeping the list empty until the complete retry succeeds', () => {
        const first = Array.from({ length: DESTINATION_PAGE_SIZE }, (_, index) => ({
            id: `list-${index + 1}`,
            name: `Shop ${index + 1}`,
            createdAt: MOCK_LIST.createdAt,
            itemsCount: 0,
        }));
        const last = { ...first[0], id: 'remote-destination', name: 'From another page', remainingCount: 0 };
        const options = service.getSelectionPage();
        const received = vi.fn();
        const failed = vi.fn();
        options.subscribe({ next: received, error: failed });
        httpMock.expectOne(`${BASE_URL}/page?page=1&limit=${String(DESTINATION_PAGE_SIZE)}`).flush(first);
        expect(received).not.toHaveBeenCalled();
        httpMock.expectOne(`${BASE_URL}/page?page=2&limit=${String(DESTINATION_PAGE_SIZE)}`).flush('offline', {
            status: HttpStatusCode.ServiceUnavailable,
            statusText: 'Unavailable',
        });
        expect(failed).toHaveBeenCalledExactlyOnceWith(expect.objectContaining({ status: HttpStatusCode.ServiceUnavailable }));
        expect(received).not.toHaveBeenCalled();

        options.subscribe({ next: received, error: failed });
        httpMock.expectOne(`${BASE_URL}/page?page=1&limit=${String(DESTINATION_PAGE_SIZE)}`).flush(first);
        httpMock.expectOne(`${BASE_URL}/page?page=2&limit=${String(DESTINATION_PAGE_SIZE)}`).flush([last]);

        expect(received).toHaveBeenCalledExactlyOnceWith([...first, last]);
        expect(failed).toHaveBeenCalledOnce();
    });

    it('loads the selected list with checked product data and pagination metadata', () => {
        const item = {
            id: 'selected-item',
            shoppingListId: MOCK_LIST.id,
            productId: 'linked-product',
            name: 'Milk',
            amount: 500,
            unit: 'Ml',
            isChecked: true,
            checkedOnUtc: '2026-01-02T01:10:00+03:00',
            sortOrder: 1,
        };
        const selected = { ...MOCK_LIST, items: [item] };
        const lists = { items: MOCK_SUMMARIES, hasMore: true, nextPage: 2 };
        const overview: ShoppingListOverviewHttpResponse = { selectedList: selected, lists };
        const received = vi.fn();
        service.getOverview().subscribe(received);
        httpMock.expectOne({ method: 'GET', url: `${BASE_URL}/overview` }).flush(overview);

        expect(received).toHaveBeenCalledExactlyOnceWith({ selectedList: selected, lists });
    });

    it('rejects an invalid selected-list revision and accepts a clean empty response on retry', () => {
        const data: ShoppingListOverviewHttpResponse = { lists: { items: [], hasMore: false } };
        const read = service.getOverview();
        const failed = vi.fn();
        const received = vi.fn();
        read.subscribe({ next: received, error: failed });
        httpMock.expectOne(`${BASE_URL}/overview`).flush({ ...data, selectedList: { ...MOCK_LIST, revision: 'invalid-revision' } });
        expect(failed).toHaveBeenCalledExactlyOnceWith(
            expect.objectContaining({ message: 'API response contains an invalid shopping list revision.' }),
        );
        expect(received).not.toHaveBeenCalled();

        read.subscribe(received);
        httpMock.expectOne(`${BASE_URL}/overview`).flush(data);
        expect(received).toHaveBeenCalledExactlyOnceWith({ selectedList: null, lists: { items: [], hasMore: false, nextPage: null } });
    });
});

describe('ShoppingListService mutations', () => {
    it('should create shopping list', () => {
        const createData: ShoppingListCreateDto = { name: 'New List' };
        const response: ShoppingList = { ...MOCK_LIST, id: entityId<'shopping-list'>('new-1'), name: 'New List' };

        service.create(createData).subscribe(result => {
            expect(result).toEqual(response);
        });

        const req = httpMock.expectOne(BASE_URL);
        expect(req.request.method).toBe('POST');
        expect(req.request.body).toEqual(createData);
        req.flush(response);
    });

    it('should update shopping list', () => {
        const updateData: ShoppingListUpdateDto = { expectedRevision: MOCK_LIST.revision, name: 'Updated List' };
        const response: ShoppingList = { ...MOCK_LIST, name: 'Updated List' };

        service.update(entityId<'shopping-list'>('abc-123'), updateData).subscribe(result => {
            expect(result).toEqual(response);
        });

        const req = httpMock.expectOne(`${BASE_URL}/abc-123`);
        expect(req.request.method).toBe('PATCH');
        expect(req.request.body).toEqual(updateData);
        req.flush(response);
    });

    it('should delete shopping list', () => {
        service.deleteById(entityId<'shopping-list'>('abc-123')).subscribe();

        const req = httpMock.expectOne(`${BASE_URL}/abc-123`);
        expect(req.request.method).toBe('DELETE');
        req.flush(null);
    });
});

describe('ShoppingListService failures', () => {
    it.each(['create', 'delete'] as const)('returns a rejected %s to its caller without repeating it', operation => {
        const failed = vi.fn();
        const write: Observable<ShoppingList | void> =
            operation === 'create' ? service.create({ name: 'Market' }) : service.deleteById(MOCK_LIST.id);
        const method = operation === 'create' ? 'POST' : 'DELETE';
        const url = operation === 'create' ? BASE_URL : `${BASE_URL}/${MOCK_LIST.id}`;
        write.subscribe({ error: failed });
        httpMock.expectOne({ method, url }).flush('offline', { status: HttpStatusCode.ServiceUnavailable, statusText: 'Unavailable' });
        expect(failed).toHaveBeenCalledExactlyOnceWith(expect.objectContaining({ status: HttpStatusCode.ServiceUnavailable }));
        httpMock.expectNone({ method, url });
    });
    it('preserves a stale-revision conflict without retrying the PATCH', () => {
        const error = vi.fn();
        service.update(MOCK_LIST.id, { expectedRevision: MOCK_LIST.revision, items: [] }).subscribe({ error });
        const request = httpMock.expectOne(`${BASE_URL}/${MOCK_LIST.id}`);
        expect(request.request.body).toEqual({ expectedRevision: MOCK_LIST.revision, items: [] });
        request.flush({ code: 'ShoppingList.RevisionConflict' }, { status: HttpStatusCode.Conflict, statusText: 'Conflict' });
        expect(error).toHaveBeenCalledExactlyOnceWith(expect.objectContaining({ status: HttpStatusCode.Conflict }));
        httpMock.expectNone(`${BASE_URL}/${MOCK_LIST.id}`);
    });
    it('should return null on getCurrent failure', () => {
        service.getCurrent().subscribe(result => {
            expect(result).toBeNull();
        });

        const req = httpMock.expectOne(`${BASE_URL}/current`);
        req.flush('Server Error', { status: HttpStatusCode.InternalServerError, statusText: 'Internal Server Error' });
    });

    it.each([HttpStatusCode.InternalServerError, HttpStatusCode.ServiceUnavailable])('should propagate page failure %s', status => {
        const next = vi.fn();
        const error = vi.fn();
        service.getPage().subscribe({ next, error });

        const req = httpMock.expectOne(`${BASE_URL}/page?page=1&limit=20&search=`);
        req.flush('Server Error', { status, statusText: 'Server Error' });
        expect(next).not.toHaveBeenCalled();
        expect(error).toHaveBeenCalledExactlyOnceWith(expect.objectContaining({ status }));
    });

    it('should return null on getById failure', () => {
        service.getById(entityId<'shopping-list'>('abc-123')).subscribe(result => {
            expect(result).toBeNull();
        });

        const req = httpMock.expectOne(`${BASE_URL}/abc-123`);
        req.flush('Not Found', { status: 404, statusText: 'Not Found' });
    });
});
