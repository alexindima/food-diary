import { TestBed } from '@angular/core/testing';
import { FormBuilder } from '@angular/forms';
import { of } from 'rxjs';
import { beforeEach, describe, expect, it, vi } from 'vitest';

import { CyclesService } from '../api/cycles.service';
import type { CycleDay, CycleResponse } from '../models/cycle.data';
import { CycleTrackingFacade } from './cycle-tracking.facade';

let facade: CycleTrackingFacade;
let cyclesService: {
    create: ReturnType<typeof vi.fn<CyclesService['create']>>;
    getCurrent: ReturnType<typeof vi.fn<CyclesService['getCurrent']>>;
    upsertDay: ReturnType<typeof vi.fn<CyclesService['upsertDay']>>;
};

beforeEach(() => {
    cyclesService = {
        getCurrent: vi.fn<CyclesService['getCurrent']>().mockReturnValue(of(createCycleResponse())),
        create: vi.fn<CyclesService['create']>().mockReturnValue(
            of({
                ...createCycleResponse(),
                id: 'cycle-2',
                startDate: '2026-04-03T00:00:00Z',
                averageLength: 30,
                lutealLength: 15,
                predictions: null,
            }),
        ),
        upsertDay: vi.fn<CyclesService['upsertDay']>().mockReturnValue(of(createCycleDayResponse())),
    };

    TestBed.configureTestingModule({
        providers: [FormBuilder, CycleTrackingFacade, { provide: CyclesService, useValue: cyclesService }],
    });

    facade = TestBed.inject(CycleTrackingFacade);
});

describe('CycleTrackingFacade current cycle', () => {
    it('loads current cycle on initialize', () => {
        facade.initialize();

        expect(cyclesService.getCurrent).toHaveBeenCalledTimes(1);
        expect(facade.cycle()?.id).toBe('cycle-1');
    });

    it('creates a new cycle from form values', () => {
        facade.startCycleForm.setValue({
            startDate: '2026-04-03',
            averageLength: 30,
            lutealLength: 15,
        });

        facade.startCycle();

        expect(cyclesService.create).toHaveBeenCalledWith({
            startDate: '2026-04-03T00:00:00.000Z',
            averageLength: 30,
            lutealLength: 15,
        });
        expect(facade.cycle()?.id).toBe('cycle-2');
    });

    it('marks start cycle form as touched when invalid', () => {
        facade.startCycleForm.controls.startDate.setValue(null);

        facade.startCycle();

        expect(cyclesService.create).not.toHaveBeenCalled();
        expect(facade.startCycleForm.controls.startDate.touched).toBe(true);
    });
});

describe('CycleTrackingFacade days', () => {
    it('upserts a day and merges it into the current cycle', () => {
        facade.initialize();
        setValidDayForm();

        facade.saveDay();

        expect(cyclesService.upsertDay).toHaveBeenCalledWith('cycle-1', {
            date: '2026-04-02T00:00:00.000Z',
            isPeriod: true,
            symptoms: {
                pain: 5,
                mood: 3,
                edema: 1,
                headache: 2,
                energy: 4,
                sleepQuality: 6,
                libido: 2,
            },
            notes: 'note',
        });
        expect(facade.days()).toHaveLength(1);
        expect(facade.days()[0].id).toBe('day-1');
    });

    it('does not save a day when current cycle is missing', () => {
        facade.saveDay();

        expect(cyclesService.upsertDay).not.toHaveBeenCalled();
    });
});

describe('CycleTrackingFacade symptom values', () => {
    it('clamps symptom values before saving a day', () => {
        facade.initialize();
        facade.dayForm.setValue({
            date: '2026-04-02',
            isPeriod: false,
            pain: -1,
            mood: 99,
            edema: Number.NaN,
            headache: 2,
            energy: 4,
            sleepQuality: 6,
            libido: 2,
            notes: null,
        });

        facade.saveDay();

        const payload = cyclesService.upsertDay.mock.calls[0][1];
        expect(payload.symptoms).toMatchObject({
            pain: 0,
            mood: 9,
            edema: 0,
        });
    });
});

describe('CycleTrackingFacade day ordering', () => {
    it('sorts days newest first and replaces existing day by date', () => {
        cyclesService.getCurrent.mockReturnValue(
            of({
                ...createCycleResponse(),
                days: [
                    { ...createCycleDayResponse(), id: 'old-day', date: '2026-04-02T00:00:00.000Z' },
                    { ...createCycleDayResponse(), id: 'later-day', date: '2026-04-03T00:00:00.000Z' },
                ],
                predictions: null,
            }),
        );
        facade.initialize();
        facade.dayForm.controls.date.setValue('2026-04-02');

        facade.saveDay();

        expect(facade.days().map(day => day.id)).toEqual(['later-day', 'day-1']);
    });
});

function createCycleResponse(): CycleResponse {
    return {
        id: 'cycle-1',
        userId: 'user-1',
        startDate: '2026-04-01T00:00:00Z',
        averageLength: 28,
        lutealLength: 14,
        days: [],
        predictions: {
            nextPeriodStart: '2026-04-29T00:00:00Z',
        },
    };
}

function createCycleDayResponse(): CycleDay {
    return {
        id: 'day-1',
        cycleId: 'cycle-1',
        date: '2026-04-02T00:00:00.000Z',
        isPeriod: true,
        symptoms: {
            pain: 5,
            mood: 3,
            edema: 1,
            headache: 2,
            energy: 4,
            sleepQuality: 6,
            libido: 2,
        },
        notes: 'note',
    };
}

function setValidDayForm(): void {
    facade.dayForm.setValue({
        date: '2026-04-02',
        isPeriod: true,
        pain: 5,
        mood: 3,
        edema: 1,
        headache: 2,
        energy: 4,
        sleepQuality: 6,
        libido: 2,
        notes: 'note',
    });
}
