import { TestBed } from '@angular/core/testing';
import { FormBuilder } from '@angular/forms';
import { TranslateService } from '@ngx-translate/core';
import { of } from 'rxjs';
import { beforeEach, describe, expect, it, vi } from 'vitest';

import { UserService } from '../../../shared/api/user.service';
import { WaistEntriesService } from '../api/waist-entries.service';
import { WaistHistoryFacade } from './waist-history.facade';

const TARGET_WAIST = 78;
const UPDATED_TARGET_WAIST = 77;
const EXPECTED_WHTR = 0.46;

let facade: WaistHistoryFacade;
let waistEntriesService: {
    create: ReturnType<typeof vi.fn>;
    getEntries: ReturnType<typeof vi.fn>;
    getSummary: ReturnType<typeof vi.fn>;
    remove: ReturnType<typeof vi.fn>;
    update: ReturnType<typeof vi.fn>;
};
let userService: {
    getDesiredWaist: ReturnType<typeof vi.fn>;
    getInfo: ReturnType<typeof vi.fn>;
    updateDesiredWaist: ReturnType<typeof vi.fn>;
};

beforeEach(() => {
    waistEntriesService = createWaistEntriesServiceMock();
    userService = {
        getDesiredWaist: vi.fn().mockReturnValue(of(TARGET_WAIST)),
        getInfo: vi.fn().mockReturnValue(of({ height: 180 })),
        updateDesiredWaist: vi.fn().mockReturnValue(of(UPDATED_TARGET_WAIST)),
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
                    getCurrentLang: vi.fn(() => 'en'),
                },
            },
        ],
    });

    facade = TestBed.inject(WaistHistoryFacade);
});

describe('WaistHistoryFacade loading', () => {
    it('loads entries, summary, desired waist, and profile on initialize', () => {
        facade.initialize();
        TestBed.tick();

        expect(waistEntriesService.getEntries).toHaveBeenCalledTimes(1);
        expect(waistEntriesService.getSummary).toHaveBeenCalledTimes(1);
        expect(userService.getDesiredWaist).toHaveBeenCalledTimes(1);
        expect(userService.getInfo).toHaveBeenCalledTimes(1);
        expect(facade.entries()).toHaveLength(2);
        expect(facade.summaryPoints()).toHaveLength(1);
        expect(facade.desiredWaist()).toBe(TARGET_WAIST);
        expect(facade.form.controls.circumference.value).toBe('82');
        expect(facade.whtViewModel()?.value).toBe(EXPECTED_WHTR);
    });
});

describe('WaistHistoryFacade entries', () => {
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

    it('does not submit invalid form', () => {
        facade.form.setValue({
            date: '',
            circumference: '',
        });

        facade.submit();

        expect(waistEntriesService.create).not.toHaveBeenCalled();
        expect(waistEntriesService.update).not.toHaveBeenCalled();
        expect(facade.form.touched).toBe(true);
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

    it('cancels editing and restores latest circumference in the form', () => {
        const entry = { id: 'entry-1', userId: 'user-1', date: '2026-04-01T00:00:00Z', circumference: 82 };
        facade.entries.set([entry, { id: 'entry-2', userId: 'user-1', date: '2026-05-01T00:00:00Z', circumference: 80.5 }]);

        facade.startEdit(entry);
        facade.cancelEdit();

        expect(facade.isEditing()).toBe(false);
        expect(facade.form.controls.circumference.value).toBe('80.5');
    });

    it('deletes entry and exits edit mode when edited entry is removed', () => {
        const entry = { id: 'entry-1', userId: 'user-1', date: '2026-04-01T00:00:00Z', circumference: 82 };
        facade.startEdit(entry);

        facade.deleteEntry(entry);

        expect(waistEntriesService.remove).toHaveBeenCalledWith('entry-1');
        expect(facade.isEditing()).toBe(false);
        expect(waistEntriesService.getEntries).toHaveBeenCalledTimes(1);
        expect(waistEntriesService.getSummary).toHaveBeenCalledTimes(1);
    });
});

describe('WaistHistoryFacade ranges', () => {
    it('ignores unsupported range values', () => {
        facade.changeRange('quarter');

        expect(facade.selectedRange()).toBe('month');
    });

    it('initializes default custom range when custom range is selected', () => {
        facade.changeRange('custom');

        expect(facade.selectedRange()).toBe('custom');
        expect(facade.customRangeControl.value?.start).toBeInstanceOf(Date);
        expect(facade.customRangeControl.value?.end).toBeInstanceOf(Date);
    });
});

describe('WaistHistoryFacade desired waist', () => {
    it('saves desired waist after validation', () => {
        facade.desiredWaistControl.setValue(`${UPDATED_TARGET_WAIST}`);

        facade.saveDesiredWaist();

        expect(userService.updateDesiredWaist).toHaveBeenCalledWith(UPDATED_TARGET_WAIST);
        expect(facade.desiredWaist()).toBe(UPDATED_TARGET_WAIST);
        expect(facade.desiredWaistControl.value).toBe(`${UPDATED_TARGET_WAIST}`);
    });
});

function createWaistEntriesServiceMock(): typeof waistEntriesService {
    return {
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
}
