import { TestBed } from '@angular/core/testing';
import { FormBuilder } from '@angular/forms';
import { TranslateService } from '@ngx-translate/core';
import { of } from 'rxjs';
import { beforeEach, describe, expect, it, vi } from 'vitest';

import { UserService } from '../../../shared/api/user.service';
import { WaistEntriesService } from '../api/waist-entries.service';
import { WaistHistoryFacade } from './waist-history.facade';

describe('WaistHistoryFacade', () => {
    let facade: WaistHistoryFacade;
    let waistEntriesService: {
        getEntries: ReturnType<typeof vi.fn>;
        getSummary: ReturnType<typeof vi.fn>;
        create: ReturnType<typeof vi.fn>;
        update: ReturnType<typeof vi.fn>;
        remove: ReturnType<typeof vi.fn>;
    };
    let userService: {
        getDesiredWaist: ReturnType<typeof vi.fn>;
        getInfo: ReturnType<typeof vi.fn>;
        updateDesiredWaist: ReturnType<typeof vi.fn>;
    };

    beforeEach(() => {
        waistEntriesService = {
            getEntries: vi.fn().mockReturnValue(
                of([
                    { id: 'entry-1', userId: 'user-1', date: '2026-04-01T00:00:00Z', circumference: 82 },
                    { id: 'entry-2', userId: 'user-1', date: '2026-03-30T00:00:00Z', circumference: 83.5 },
                ]),
            ),
            getSummary: vi
                .fn()
                .mockReturnValue(of([{ startDate: '2026-04-01T00:00:00Z', endDate: '2026-04-01T23:59:59Z', averageCircumference: 82 }])),
            create: vi.fn().mockReturnValue(of({ id: 'entry-3', userId: 'user-1', date: '2026-04-02T00:00:00Z', circumference: 81.7 })),
            update: vi.fn().mockReturnValue(of({ id: 'entry-1', userId: 'user-1', date: '2026-04-01T00:00:00Z', circumference: 82 })),
            remove: vi.fn().mockReturnValue(of(void 0)),
        };
        userService = {
            getDesiredWaist: vi.fn().mockReturnValue(of(78)),
            getInfo: vi.fn().mockReturnValue(of({ height: 180 })),
            updateDesiredWaist: vi.fn().mockReturnValue(of(77)),
        };

        TestBed.configureTestingModule({
            providers: [
                FormBuilder,
                WaistHistoryFacade,
                { provide: WaistEntriesService, useValue: waistEntriesService },
                { provide: UserService, useValue: userService },
                {
                    provide: TranslateService,
                    useValue: {
                        instant: vi.fn((key: string) => key),
                    },
                },
            ],
        });

        facade = TestBed.inject(WaistHistoryFacade);
    });

    it('loads entries, summary, desired waist, and profile on initialize', () => {
        facade.initialize();
        TestBed.tick();

        expect(waistEntriesService.getEntries).toHaveBeenCalledTimes(1);
        expect(waistEntriesService.getSummary).toHaveBeenCalledTimes(1);
        expect(userService.getDesiredWaist).toHaveBeenCalledTimes(1);
        expect(userService.getInfo).toHaveBeenCalledTimes(1);
        expect(facade.entries()).toHaveLength(2);
        expect(facade.summaryPoints()).toHaveLength(1);
        expect(facade.desiredWaist()).toBe(78);
        expect(facade.whtrValue()).toBe(0.46);
    });

    it('submits a new entry and reloads the list', () => {
        facade.initialize();
        TestBed.tick();
        waistEntriesService.getEntries.mockClear();
        waistEntriesService.getSummary.mockClear();

        facade.form.setValue({
            date: '2026-04-02',
            circumference: '81.7',
        });

        facade.submit();

        expect(waistEntriesService.create).toHaveBeenCalledWith({
            date: '2026-04-02T00:00:00.000Z',
            circumference: 81.7,
        });
        expect(waistEntriesService.getEntries).toHaveBeenCalledTimes(1);
        expect(waistEntriesService.getSummary).toHaveBeenCalledTimes(1);
    });

    it('switches to edit mode and updates the existing entry', () => {
        const entry = { id: 'entry-1', userId: 'user-1', date: '2026-04-01T00:00:00Z', circumference: 82 };

        facade.startEdit(entry);
        facade.submit();

        expect(facade.isEditing()).toBe(false);
        expect(waistEntriesService.update).toHaveBeenCalledWith('entry-1', {
            date: '2026-04-01T00:00:00.000Z',
            circumference: 82,
        });
    });

    it('saves desired waist after validation', () => {
        facade.desiredWaistControl.setValue('77');

        facade.saveDesiredWaist();

        expect(userService.updateDesiredWaist).toHaveBeenCalledWith(77);
        expect(facade.desiredWaist()).toBe(77);
        expect(facade.desiredWaistControl.value).toBe('77');
    });
});
