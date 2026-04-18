import { beforeEach, describe, expect, it, vi } from 'vitest';
import { TestBed } from '@angular/core/testing';
import { FormBuilder } from '@angular/forms';
import { of } from 'rxjs';
import { CyclesService } from '../api/cycles.service';
import { CycleTrackingFacade } from './cycle-tracking.facade';

describe('CycleTrackingFacade', () => {
    let facade: CycleTrackingFacade;
    let cyclesService: {
        getCurrent: ReturnType<typeof vi.fn>;
        create: ReturnType<typeof vi.fn>;
        upsertDay: ReturnType<typeof vi.fn>;
    };

    beforeEach(() => {
        cyclesService = {
            getCurrent: vi.fn().mockReturnValue(
                of({
                    id: 'cycle-1',
                    userId: 'user-1',
                    startDate: '2026-04-01T00:00:00Z',
                    averageLength: 28,
                    lutealLength: 14,
                    days: [],
                    predictions: {
                        nextPeriodStart: '2026-04-29T00:00:00Z',
                    },
                }),
            ),
            create: vi.fn().mockReturnValue(
                of({
                    id: 'cycle-2',
                    userId: 'user-1',
                    startDate: '2026-04-03T00:00:00Z',
                    averageLength: 30,
                    lutealLength: 15,
                    days: [],
                    predictions: null,
                }),
            ),
            upsertDay: vi.fn().mockReturnValue(
                of({
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
                }),
            ),
        };

        TestBed.configureTestingModule({
            providers: [FormBuilder, CycleTrackingFacade, { provide: CyclesService, useValue: cyclesService }],
        });

        facade = TestBed.inject(CycleTrackingFacade);
    });

    it('loads current cycle on initialize', () => {
        facade.initialize();

        expect(cyclesService.getCurrent).toHaveBeenCalledTimes(1);
        expect(facade.cycle()?.id).toBe('cycle-1');
        expect(facade.currentCycleTitle()).toBe('CYCLE_TRACKING.CURRENT_CYCLE');
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

    it('upserts a day and merges it into the current cycle', () => {
        facade.initialize();
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
});
