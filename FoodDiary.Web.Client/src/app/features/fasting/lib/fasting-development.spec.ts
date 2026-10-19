import { TestBed } from '@angular/core/testing';
import { TranslateService } from '@ngx-translate/core';
import { FdUiToastService } from 'fd-ui-kit/toast/fd-ui-toast.service';
import { beforeEach, describe, expect, it, vi } from 'vitest';

import { FrontendObservabilityService } from '../../../services/frontend-observability.service';
import { UserService } from '../../../shared/api/user.service';
import { DEVELOPMENT_SCREENS, type DevelopmentScreen } from '../../../shared/development/development-tools';
import { utcInstant } from '../../../shared/models/semantics/date-value';
import { entityId } from '../../../shared/models/semantics/entity-id';
import { FastingService } from '../api/fasting.service';
import { FastingFacade } from './fasting.facade';
import { connectFastingDevelopmentSetup, readFastingDevelopmentContext } from './fasting-development';

describe('unsaved fasting preparation', () => {
    let facade: FastingFacade;
    let screen: DevelopmentScreen;
    const start = vi.fn();
    beforeEach(() => {
        start.mockReset();
        TestBed.configureTestingModule({
            providers: [
                FastingFacade,
                { provide: FastingService, useValue: { start } },
                { provide: FrontendObservabilityService, useValue: {} },
                { provide: UserService, useValue: { user: (): null => null } },
                { provide: TranslateService, useValue: {} },
                { provide: FdUiToastService, useValue: {} },
                {
                    provide: DEVELOPMENT_SCREENS,
                    useValue: {
                        attach: (value: DevelopmentScreen) => {
                            screen = value;
                            return (): void => {};
                        },
                    },
                },
            ],
        });
        facade = TestBed.inject(FastingFacade);
        TestBed.runInInjectionContext(() => {
            connectFastingDevelopmentSetup(facade);
        });
    });
    it('prepares cyclic 1:2 using owner selections and restores the previous setup without starting', () => {
        const baseline = screen.read();
        screen.fillDraft?.({ mode: 'cyclic', fastHours: 16, fastDays: 1, eatDays: 2 });
        expect(facade.selectedMode()).toBe('cyclic');
        expect(facade.cyclicFastDays()).toBe(1);
        expect(facade.cyclicEatDays()).toBe(2);
        expect(screen.isDirty?.()).toBe(true);
        expect(start).not.toHaveBeenCalled();
        screen.clearDraft?.();
        expect(screen.read()).toEqual(baseline);
        expect(screen.isDirty?.()).toBe(false);
    });
    it.each([
        { mode: 'cyclic', fastHours: 16, fastDays: 1, eatDays: 0 },
        { mode: 'cyclic', fastHours: 16.5, fastDays: 1, eatDays: 2 },
        { mode: 'extended', fastHours: 169 },
        { mode: 'intermittent', fastHours: 24 },
    ])('rejects invalid setup atomically: %j', draft => {
        const baseline = screen.read();
        expect(() => screen.fillDraft?.(draft)).toThrow();
        expect(screen.read()).toEqual(baseline);
        expect(start).not.toHaveBeenCalled();
    });
    it('refuses preparation while the ordinary start request is in flight', () => {
        facade.isStarting.set(true);
        expect(() => screen.fillDraft?.({ mode: 'extended', fastHours: 48 })).toThrow('idle');
        expect(start).not.toHaveBeenCalled();
    });
    it('publishes active phase and duration while omitting private notes and symptoms', () => {
        facade.currentSession.set({
            id: entityId<'fasting-session'>('session-1'),
            startedAtUtc: utcInstant('2026-10-18T12:00:00Z'),
            endedAtUtc: null,
            initialPlannedDurationHours: 24,
            addedDurationHours: 0,
            plannedDurationHours: 24,
            protocol: 'Cyclic',
            planType: 'Cyclic',
            occurrenceKind: 'EatDay',
            cyclicFastDays: 1,
            cyclicEatDays: 2,
            cyclicEatDayFastHours: 16,
            cyclicEatDayEatingWindowHours: 8,
            cyclicPhaseDayNumber: 1,
            cyclicPhaseDayTotal: 2,
            isCompleted: false,
            status: 'Active',
            notes: 'private-notes',
            checkInAtUtc: null,
            hungerLevel: null,
            energyLevel: null,
            moodLevel: null,
            symptoms: ['private-symptom'],
            checkInNotes: 'private-check-in',
            checkIns: [],
        });
        const context = readFastingDevelopmentContext(facade);
        expect(context).toMatchObject({
            active: true,
            session: { occurrenceKind: 'EatDay', cyclicPhaseDayNumber: 1, cyclicPhaseDayTotal: 2, plannedDurationHours: 24 },
        });
        expect(JSON.stringify(context)).not.toContain('private-');
    });
});
