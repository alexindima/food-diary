import { TestBed } from '@angular/core/testing';
import { Router } from '@angular/router';
import { of, Subject } from 'rxjs';
import { beforeEach, describe, expect, it, vi } from 'vitest';

import { waitForAsyncTasksAsync } from '../../../testing/async-testing';
import type { MealDevelopmentScreen } from '../../features/meals/contracts/meal-development-tools';
import { ProductService } from '../../features/products/api/product.service';
import { AuthService } from '../../services/auth.service';
import { isRecord } from '../../shared/lib/unknown-value.utils';
import { createEmptyProductSnapshot } from '../../shared/models/meal.data';
import type { PageOf } from '../../shared/models/page-of.data';
import type { Product } from '../../shared/models/product.data';
import { LocalWebmcpPilotService } from './local-webmcp-pilot.service';

const SEARCH_LIMIT = 25;
const draft = {
    date: '2026-10-17',
    time: '00:15',
    mealType: 'SNACK',
    comment: 'Pilot',
    items: [{ productName: 'Apple', amount: 150, unit: 'G' }],
};

let service: LocalWebmcpPilotService;
let router: { url: string; navigate: ReturnType<typeof vi.fn<Router['navigate']>> };
let auth: {
    isAuthenticated: ReturnType<typeof vi.fn>;
    getUserId: ReturnType<typeof vi.fn>;
    isAdmin: ReturnType<typeof vi.fn>;
    isDietologist: ReturnType<typeof vi.fn>;
    isEmailConfirmed: ReturnType<typeof vi.fn>;
};
let catalog: { query: ReturnType<typeof vi.fn> };
const product = { ...createEmptyProductSnapshot(), name: 'Apple' };
const page: PageOf<Product> = { data: [product], page: 1, limit: SEARCH_LIMIT, totalPages: 1, totalItems: 1 };

beforeEach(() => {
    router = { url: '/meals?code=private#private', navigate: vi.fn<Router['navigate']>().mockResolvedValue(true) };
    auth = {
        isAuthenticated: vi.fn().mockReturnValue(true),
        getUserId: vi.fn().mockReturnValue('owned-user'),
        isAdmin: vi.fn().mockReturnValue(false),
        isDietologist: vi.fn().mockReturnValue(false),
        isEmailConfirmed: vi.fn().mockReturnValue(true),
    };
    catalog = { query: vi.fn().mockReturnValue(of(page)) };
    TestBed.configureTestingModule({
        providers: [
            { provide: Router, useValue: router },
            { provide: AuthService, useValue: auth },
            { provide: ProductService, useValue: catalog },
        ],
    });
    service = TestBed.inject(LocalWebmcpPilotService);
});

const callAsync = async (pilot: LocalWebmcpPilotService, name: string, input: unknown): Promise<unknown> => {
    const tool = pilot['tools']('owned-user').find(value => value.name === name);
    if (tool === undefined) {
        throw new Error('Missing tool');
    }
    return tool.execute(input);
};

function expectToolError(result: unknown, message = ''): void {
    expect(isRecord(result)).toBe(true);
    if (isRecord(result)) {
        expect(result['ok']).toBe(false);
        expect(typeof result['error']).toBe('string');
        expect(result['error']).toContain(message);
    }
}

describe('local WebMCP screen and navigation', () => {
    it('reads only explicitly attached screen state, excluding arbitrary URL values', async () => {
        service.attach({ read: () => ({ kind: 'meal-diary', dateFrom: '2026-10-17' }) });
        expect(await callAsync(service, 'fooddiary_read_screen_context', {})).toMatchObject({
            path: '/meals',
            screen: { kind: 'meal-diary', dateFrom: '2026-10-17' },
        });
        expect(catalog.query).not.toHaveBeenCalled();
    });

    it('does not let disposal of an old page clear the new screen', async () => {
        const dispose = service.attach({ read: () => 'old' });
        service.attach({ read: () => 'current' });
        dispose();
        expect(await callAsync(service, 'fooddiary_read_screen_context', {})).toMatchObject({ path: '/meals', screen: 'current' });
    });

    it('opens a calendar date using the existing diary URL contract', async () => {
        await callAsync(service, 'fooddiary_open_diary_date', { date: '2028-02-29' });
        expect(router.navigate).toHaveBeenCalledWith(['/meals'], { queryParams: { dateFrom: '2028-02-29', dateTo: '2028-02-29' } });
    });

    it('treats opening the current diary date as a successful idempotent operation', async () => {
        router.url = '/meals?dateFrom=2026-10-17&dateTo=2026-10-17';
        service.attach({ read: () => ({ kind: 'meal-diary', dateFrom: '2026-10-17' }) });
        expect(await callAsync(service, 'fooddiary_open_diary_date', { date: '2026-10-17' })).toMatchObject({
            path: '/meals',
            screen: { kind: 'meal-diary', dateFrom: '2026-10-17' },
        });
        expect(router.navigate).not.toHaveBeenCalled();
    });

    it('refuses navigation and draft replacement when the editor is dirty', async () => {
        service.attach({ read: () => ({}), isDirty: () => true });
        expectToolError(await callAsync(service, 'fooddiary_open_diary_date', { date: draft.date }), 'Unsaved');
        expectToolError(await callAsync(service, 'fooddiary_fill_meal_draft', draft), 'Unsaved');
        expect(router.navigate).not.toHaveBeenCalled();
        expect(catalog.query).not.toHaveBeenCalled();
    });
});
describe('local WebMCP draft preparation', () => {
    it('allows context inspection during a pending draft lookup while serializing writes', async () => {
        const products = new Subject<PageOf<Product>>();
        catalog.query.mockReturnValue(products);
        const fillDraft = vi.fn();
        service.attach({ read: () => ({ kind: 'new-meal-editor', loading: true }), isDirty: () => false, fillDraft });
        const pending = callAsync(service, 'fooddiary_fill_meal_draft', draft);
        await vi.waitFor(() => {
            expect(catalog.query).toHaveBeenCalledOnce();
        });
        expect(await callAsync(service, 'fooddiary_read_screen_context', {})).toMatchObject({
            screen: { kind: 'new-meal-editor', loading: true },
        });
        expectToolError(await callAsync(service, 'fooddiary_open_diary_date', { date: draft.date }), 'Another');
        products.next(page);
        products.complete();
        await pending;
        expect(fillDraft).toHaveBeenCalledOnce();
    });
    it('fills the real editor through its capability without a write API', async () => {
        const fillDraft = vi.fn();
        const editor: MealDevelopmentScreen = { read: () => ({ kind: 'new-meal-editor' }), isDirty: () => false, fillDraft };
        router.navigate.mockImplementation(async () => {
            await waitForAsyncTasksAsync();
            service.attach(editor);
            return true;
        });
        expect(await callAsync(service, 'fooddiary_fill_meal_draft', draft)).toMatchObject({
            saved: false,
            path: '/meals',
            screen: { kind: 'new-meal-editor' },
        });
        expect(fillDraft).toHaveBeenCalledWith({ ...draft, items: [{ product, amount: 150 }] }, expect.any(Function));
        expect(catalog.query).toHaveBeenCalledWith(1, SEARCH_LIMIT, { search: 'Apple' });
    });

    it.each(['unit', 'ambiguous', 'missing'])('rejects %s product matches before navigation', async reason => {
        if (reason === 'unit') {
            draft.items[0].unit = 'ML';
        }
        if (reason === 'ambiguous') {
            catalog.query.mockReturnValue(of({ ...page, data: [product, product] }));
        }
        if (reason === 'missing') {
            catalog.query.mockReturnValue(of({ ...page, data: [] }));
        }
        try {
            expectToolError(await callAsync(service, 'fooddiary_fill_meal_draft', draft));
        } finally {
            draft.items[0].unit = 'G';
        }
        expect(router.navigate).not.toHaveBeenCalled();
    });

    it('rejects an expired session and a session change during product loading', async () => {
        auth.isAuthenticated.mockReturnValue(false);
        expectToolError(await callAsync(service, 'fooddiary_read_screen_context', {}), 'session');
        auth.isAuthenticated.mockReturnValue(true);
        const response = new Subject<PageOf<Product>>();
        catalog.query.mockReturnValue(response);
        const result = callAsync(service, 'fooddiary_fill_meal_draft', draft);
        await vi.waitFor(() => {
            expect(catalog.query).toHaveBeenCalled();
        });
        auth.getUserId.mockReturnValue('another-user');
        response.next(page);
        expectToolError(await result, 'session');
        expect(router.navigate).not.toHaveBeenCalled();
    });
});
