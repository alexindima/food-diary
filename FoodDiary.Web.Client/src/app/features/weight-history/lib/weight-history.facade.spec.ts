import { TestBed } from '@angular/core/testing';
import { FormBuilder } from '@angular/forms';
import { TranslateService } from '@ngx-translate/core';
import { of } from 'rxjs';
import { beforeEach, describe, expect, it, vi } from 'vitest';

import { UserService } from '../../../shared/api/user.service';
import { WeightEntriesService } from '../api/weight-entries.service';
import { WeightHistoryFacade } from './weight-history.facade';

const TARGET_WEIGHT = 70;
const UPDATED_TARGET_WEIGHT = 69;
const EXPECTED_BMI = 22.9;

describe('WeightHistoryFacade', () => {
    let facade: WeightHistoryFacade;
    let weightEntriesService: {
        getEntries: ReturnType<typeof vi.fn>;
        getSummary: ReturnType<typeof vi.fn>;
        create: ReturnType<typeof vi.fn>;
        update: ReturnType<typeof vi.fn>;
        remove: ReturnType<typeof vi.fn>;
    };
    let userService: {
        getDesiredWeight: ReturnType<typeof vi.fn>;
        getInfo: ReturnType<typeof vi.fn>;
        updateDesiredWeight: ReturnType<typeof vi.fn>;
    };

    beforeEach(() => {
        weightEntriesService = {
            getEntries: vi.fn().mockReturnValue(
                of([
                    { id: 'entry-1', userId: 'user-1', date: '2026-04-01T00:00:00Z', weight: 74.2 },
                    { id: 'entry-2', userId: 'user-1', date: '2026-03-30T00:00:00Z', weight: 75.1 },
                ]),
            ),
            getSummary: vi
                .fn()
                .mockReturnValue(of([{ startDate: '2026-04-01T00:00:00Z', endDate: '2026-04-01T23:59:59Z', averageWeight: 74.2 }])),
            create: vi.fn().mockReturnValue(of({ id: 'entry-3', userId: 'user-1', date: '2026-04-02T00:00:00Z', weight: 73.8 })),
            update: vi.fn().mockReturnValue(of({ id: 'entry-1', userId: 'user-1', date: '2026-04-01T00:00:00Z', weight: 74.2 })),
            remove: vi.fn().mockReturnValue(of(void 0)),
        };
        userService = {
            getDesiredWeight: vi.fn().mockReturnValue(of(TARGET_WEIGHT)),
            getInfo: vi.fn().mockReturnValue(of({ height: 180 })),
            updateDesiredWeight: vi.fn().mockReturnValue(of(UPDATED_TARGET_WEIGHT)),
        };

        TestBed.configureTestingModule({
            providers: [
                FormBuilder,
                WeightHistoryFacade,
                { provide: WeightEntriesService, useValue: weightEntriesService },
                { provide: UserService, useValue: userService },
                {
                    provide: TranslateService,
                    useValue: {
                        instant: vi.fn((key: string) => key),
                    },
                },
            ],
        });

        facade = TestBed.inject(WeightHistoryFacade);
    });

    it('loads entries, summary, desired weight, and profile on initialize', () => {
        facade.initialize();
        TestBed.tick();

        expect(weightEntriesService.getEntries).toHaveBeenCalledTimes(1);
        expect(weightEntriesService.getSummary).toHaveBeenCalledTimes(1);
        expect(userService.getDesiredWeight).toHaveBeenCalledTimes(1);
        expect(userService.getInfo).toHaveBeenCalledTimes(1);
        expect(facade.entries()).toHaveLength(2);
        expect(facade.summaryPoints()).toHaveLength(1);
        expect(facade.desiredWeight()).toBe(TARGET_WEIGHT);
        expect(facade.form.controls.weight.value).toBe('74.2');
        expect(facade.bmiValue()).toBe(EXPECTED_BMI);
    });

    it('submits a new entry and reloads the list', () => {
        facade.initialize();
        TestBed.tick();
        weightEntriesService.getEntries.mockClear();
        weightEntriesService.getSummary.mockClear();

        facade.form.setValue({
            date: '2026-04-02',
            weight: '73.8',
        });

        facade.submit();

        expect(weightEntriesService.create).toHaveBeenCalledWith({
            date: '2026-04-02T00:00:00.000Z',
            weight: 73.8,
        });
        expect(weightEntriesService.getEntries).toHaveBeenCalledTimes(1);
        expect(weightEntriesService.getSummary).toHaveBeenCalledTimes(1);
    });

    it('switches to edit mode and updates the existing entry', () => {
        const entry = { id: 'entry-1', userId: 'user-1', date: '2026-04-01T00:00:00Z', weight: 74.2 };

        facade.startEdit(entry);
        facade.submit();

        expect(facade.isEditing()).toBe(false);
        expect(weightEntriesService.update).toHaveBeenCalledWith('entry-1', {
            date: '2026-04-01T00:00:00.000Z',
            weight: 74.2,
        });
    });

    it('saves desired weight after validation', () => {
        facade.desiredWeightControl.setValue(`${UPDATED_TARGET_WEIGHT}`);

        facade.saveDesiredWeight();

        expect(userService.updateDesiredWeight).toHaveBeenCalledWith(UPDATED_TARGET_WEIGHT);
        expect(facade.desiredWeight()).toBe(UPDATED_TARGET_WEIGHT);
        expect(facade.desiredWeightControl.value).toBe(`${UPDATED_TARGET_WEIGHT}`);
    });
});
