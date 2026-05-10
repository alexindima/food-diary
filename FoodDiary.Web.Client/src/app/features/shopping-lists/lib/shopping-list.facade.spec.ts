import { TestBed } from '@angular/core/testing';
import { TranslateService } from '@ngx-translate/core';
import { FdUiToastService } from 'fd-ui-kit/toast/fd-ui-toast.service';
import { of } from 'rxjs';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';

import { MeasurementUnit } from '../../products/models/product.data';
import { ShoppingListService } from '../api/shopping-list.service';
import type { ShoppingList } from '../models/shopping-list.data';
import { ShoppingListFacade } from './shopping-list.facade';

describe('ShoppingListFacade', () => {
    let facade: ShoppingListFacade;
    let shoppingListService: {
        getAll: ReturnType<typeof vi.fn>;
        getById: ReturnType<typeof vi.fn>;
        create: ReturnType<typeof vi.fn>;
        update: ReturnType<typeof vi.fn>;
        deleteById: ReturnType<typeof vi.fn>;
    };
    let toastService: { open: ReturnType<typeof vi.fn>; error: ReturnType<typeof vi.fn> };

    const list: ShoppingList = {
        id: 'list-1',
        name: 'Main list',
        createdAt: '2026-01-01T00:00:00Z',
        items: [],
    };

    beforeEach(() => {
        vi.useFakeTimers();

        shoppingListService = {
            getAll: vi.fn(),
            getById: vi.fn(),
            create: vi.fn(),
            update: vi.fn(),
            deleteById: vi.fn(),
        };

        shoppingListService.getAll.mockReturnValue(of([{ id: 'list-1', name: 'Main list', createdAt: '', itemsCount: 0 }]));
        shoppingListService.getById.mockReturnValue(of(list));
        shoppingListService.create.mockReturnValue(of(list));
        shoppingListService.update.mockReturnValue(of(list));
        shoppingListService.deleteById.mockReturnValue(of(undefined));
        toastService = { open: vi.fn(), error: vi.fn() };

        TestBed.configureTestingModule({
            providers: [
                ShoppingListFacade,
                { provide: ShoppingListService, useValue: shoppingListService },
                { provide: TranslateService, useValue: { instant: (key: string): string => key } },
                { provide: FdUiToastService, useValue: toastService },
            ],
        });

        facade = TestBed.inject(ShoppingListFacade);
    });

    afterEach(() => {
        vi.useRealTimers();
    });

    it('should load lists and current list on initialize', () => {
        facade.initialize();

        expect(shoppingListService.getAll).toHaveBeenCalledTimes(1);
        expect(shoppingListService.getById).toHaveBeenCalledWith('list-1');
        expect(facade.list()?.id).toBe('list-1');
        expect(facade.selectedListId()).toBe('list-1');
    });

    it('should add item and persist after debounce', async () => {
        facade.initialize();
        await Promise.resolve();

        facade.addItem({
            name: 'Milk',
            amount: 1,
            unit: MeasurementUnit.ML,
            category: 'Dairy',
        });

        expect(facade.items()).toHaveLength(1);

        vi.advanceTimersByTime(500);

        expect(shoppingListService.update).toHaveBeenCalledTimes(1);
    });

    it('should persist renamed list after debounce', async () => {
        facade.initialize();
        await Promise.resolve();

        facade.setListName('Renamed list');
        vi.advanceTimersByTime(500);

        expect(shoppingListService.update).toHaveBeenCalledWith('list-1', expect.objectContaining({ name: 'Renamed list' }));
    });
});
