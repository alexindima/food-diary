import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { TranslateService } from '@ngx-translate/core';
import { Subject } from 'rxjs';
import { beforeEach, describe, expect, it, vi } from 'vitest';

import { provideTranslateTesting } from '../../../../../testing/translate-testing.module';
import type { FastingSession } from '../../models/fasting.data';
import { FastingRedesignPreviewComponent } from './fasting-redesign-preview';

const INTERMITTENT_HOURS = 16;
const TEST_PROGRESS_PERCENT = 6.25;
const TUESDAY_AFTERNOON = '2026-08-11T18:00:00';
const TUESDAY_MORNING_START = '2026-08-11T09:00:00';
const TUESDAY_MORNING_END = '2026-08-11T10:00:00';
const TUESDAY_MIDDAY_START = '2026-08-11T11:00:00';
const TUESDAY_MIDDAY_END = '2026-08-11T12:00:00';
const TUESDAY_AFTERNOON_START = '2026-08-11T13:00:00';
const TUESDAY_AFTERNOON_END = '2026-08-11T14:00:00';
const COMPLETED_SESSION_COUNT = 2;
const TEST_SESSION_COUNT = 3;
const PERCENT_MULTIPLIER = 100;
const EXPECTED_TUESDAY_PROGRESS = (COMPLETED_SESSION_COUNT / TEST_SESSION_COUNT) * PERCENT_MULTIPLIER;

// eslint-disable-next-line max-lines-per-function -- protocol and weekly-calendar scenarios share the same required-input setup.
describe('FastingRedesignPreviewComponent', () => {
    let fixture: ComponentFixture<FastingRedesignPreviewComponent>;
    let component: FastingRedesignPreviewComponent;
    let languageChanges: Subject<unknown>;
    let hoursLabel: string;

    beforeEach(async () => {
        languageChanges = new Subject<unknown>();
        hoursLabel = 'ч';

        await TestBed.configureTestingModule({
            imports: [FastingRedesignPreviewComponent],
            providers: [
                {
                    provide: TranslateService,
                    useValue: {
                        instant: (key: string, params?: Record<string, number>): string => {
                            if (key === 'FASTING.HOURS') {
                                return hoursLabel;
                            }

                            return key === 'FASTING.REDESIGN.SESSIONS_COUNT' ? `Сессий: ${params?.['count'] ?? 0}` : key;
                        },
                        onLangChange: languageChanges,
                    },
                },
            ],
        })
            .overrideComponent(FastingRedesignPreviewComponent, { set: { template: '' } })
            .compileComponents();

        fixture = TestBed.createComponent(FastingRedesignPreviewComponent);
        component = fixture.componentInstance;
        fixture.componentRef.setInput('stats', null);
        fixture.componentRef.setInput('history', []);
        fixture.componentRef.setInput('elapsedFormatted', '01:00:00');
        fixture.componentRef.setInput('remainingFormatted', '15:00:00');
        fixture.componentRef.setInput('progressPercent', TEST_PROGRESS_PERCENT);
        fixture.componentRef.setInput('now', new Date('2026-08-11T16:00:00Z'));
        fixture.componentRef.setInput('selectedDurationHours', INTERMITTENT_HOURS);
    });

    it('describes an intermittent fast and its upcoming eating window', () => {
        fixture.componentRef.setInput('session', createSession());

        expect(component['phaseLabelKey']()).toBe('FASTING.FASTING_WINDOW');
        expect(component['nextPhaseLabelKey']()).toBe('FASTING.EATING_WINDOW');
        expect(component['flowView']()).toEqual({
            key: 'FASTING.REDESIGN.INTERMITTENT_FLOW',
            params: { fastHours: 16, eatHours: 8 },
        });
    });

    it('keeps the active session first in recent sessions without duplicating it', () => {
        const activeSession = createSession({ id: 'active-session' });
        fixture.componentRef.setInput('session', activeSession);
        fixture.componentRef.setInput('history', [
            activeSession,
            createSession({ id: 'completed-session', status: 'Completed', endedAtUtc: '2026-08-11T14:00:00Z' }),
        ]);

        expect(component['recentSessions']().map(session => session.id)).toEqual(['active-session', 'completed-session']);
    });

    it('shows no wellbeing result until the session has a check-in', () => {
        fixture.componentRef.setInput('session', createSession({ moodLevel: 5 }));

        expect(component['wellbeingKey']()).toBe('FASTING.REDESIGN.WELLBEING_EMPTY');
    });

    it('calculates wellbeing from all check-in dimensions', () => {
        fixture.componentRef.setInput(
            'session',
            createSession({
                checkIns: [
                    {
                        id: 'check-in-1',
                        checkedInAtUtc: '2026-08-11T16:00:00Z',
                        hungerLevel: 4,
                        energyLevel: 5,
                        moodLevel: 4,
                        symptoms: [],
                        notes: null,
                    },
                ],
            }),
        );

        expect(component['wellbeingKey']()).toBe('FASTING.REDESIGN.WELLBEING_GOOD');
    });

    it('describes an extended fast without promising a following eating phase', () => {
        fixture.componentRef.setInput('session', createSession({ planType: 'Extended', plannedDurationHours: 36 }));

        expect(component['phaseLabelKey']()).toBe('FASTING.EXTENDED_TYPE');
        expect(component['nextPhaseLabelKey']()).toBeNull();
        expect(component['flowView']()).toEqual({ key: 'FASTING.REDESIGN.EXTENDED_FLOW', params: { hours: 36 } });
        expect(component['protocolLabel']()).toBe('36 ч');
    });

    it('updates the extended protocol unit when the language changes', () => {
        fixture.componentRef.setInput('session', createSession({ planType: 'Extended', plannedDurationHours: 72 }));
        expect(component['protocolLabel']()).toBe('72 ч');

        hoursLabel = 'h';
        languageChanges.next({ lang: 'en' });

        expect(component['protocolLabel']()).toBe('72 h');
    });

    it('describes the current and next phases of a cyclic session', () => {
        const cyclicSession = createSession({
            planType: 'Cyclic',
            occurrenceKind: 'EatingWindow',
            cyclicFastDays: 2,
            cyclicEatDays: 1,
        });
        fixture.componentRef.setInput('session', cyclicSession);

        expect(component['phaseLabelKey']()).toBe('FASTING.EATING_WINDOW');
        expect(component['nextPhaseLabelKey']()).toBe('FASTING.FASTING_WINDOW');
        expect(component['flowView']()).toEqual({
            key: 'FASTING.REDESIGN.CYCLIC_FLOW',
            params: { fastDays: 2, eatDays: 1 },
        });
        expect(component['historyTypeLabelKey'](cyclicSession)).toBe('FASTING.CYCLIC_TYPE');
        expect(component['historyProtocolLabel'](cyclicSession)).toBe('2:1 · 16:8');
    });

    it('builds detailed labels for recent intermittent and extended sessions', () => {
        const intermittent = createSession({ status: 'Completed', endedAtUtc: '2026-08-11T16:00:00Z' });
        const extended = createSession({
            planType: 'Extended',
            plannedDurationHours: 36,
            status: 'Completed',
            endedAtUtc: '2026-08-11T16:00:00Z',
        });

        expect(component['historyTypeLabelKey'](intermittent)).toBe('FASTING.INTERMITTENT_TYPE');
        expect(component['historyProtocolLabel'](intermittent)).toBe('16:8');
        expect(component['historyTypeLabelKey'](extended)).toBe('FASTING.EXTENDED_TYPE');
        expect(component['historyProtocolLabel'](extended)).toBe(`36 ${hoursLabel}`);
    });

    it('groups sessions by their local calendar day in the current week', () => {
        fixture.componentRef.setInput('session', null);
        fixture.componentRef.setInput('now', new Date(TUESDAY_AFTERNOON));
        fixture.componentRef.setInput('history', [
            createSession({
                id: 'completed-1',
                startedAtUtc: new Date(TUESDAY_MORNING_START).toISOString(),
                endedAtUtc: new Date(TUESDAY_MORNING_END).toISOString(),
                status: 'Completed',
            }),
            createSession({
                id: 'completed-2',
                startedAtUtc: new Date(TUESDAY_MIDDAY_START).toISOString(),
                endedAtUtc: new Date(TUESDAY_MIDDAY_END).toISOString(),
                status: 'Completed',
            }),
            createSession({
                id: 'interrupted-1',
                startedAtUtc: new Date(TUESDAY_AFTERNOON_START).toISOString(),
                endedAtUtc: new Date(TUESDAY_AFTERNOON_END).toISOString(),
                status: 'Interrupted',
            }),
        ]);

        const days = component['rhythmDays']();

        expect(days.map(day => day.durationLabel)).toEqual(['-', 'Сессий: 3', '-', '-', '-', '-', '-']);
        expect(days[1]).toMatchObject({ dayKey: 'FASTING.REDESIGN.DAY_2', completed: false });
        expect(days[1].progress).toBeCloseTo(EXPECTED_TUESDAY_PROGRESS);
    });
});

// eslint-disable-next-line max-lines-per-function -- notes, alerts, and insights exercise the same fully rendered component contract.
describe('FastingRedesignPreviewComponent notes', () => {
    it('keeps active notes visible and opens historical session details from its row', async () => {
        await TestBed.configureTestingModule({
            imports: [FastingRedesignPreviewComponent],
            providers: [provideTranslateTesting()],
        }).compileComponents();

        const fixture = TestBed.createComponent(FastingRedesignPreviewComponent);
        fixture.componentRef.setInput(
            'session',
            createSession({ notes: 'Active session note', checkInNotes: 'Active wellbeing note', moodLevel: 4 }),
        );
        fixture.componentRef.setInput('stats', null);
        fixture.componentRef.setInput('history', [
            createSession({
                id: 'history-with-notes',
                endedAtUtc: '2026-08-11T16:00:00Z',
                status: 'Completed',
                notes: 'Historical session note',
                checkInNotes: 'Historical wellbeing note',
            }),
        ]);
        fixture.componentRef.setInput('elapsedFormatted', '01:00:00');
        fixture.componentRef.setInput('remainingFormatted', '15:00:00');
        fixture.componentRef.setInput('progressPercent', TEST_PROGRESS_PERCENT);
        fixture.componentRef.setInput('now', new Date('2026-08-11T16:00:00Z'));
        fixture.componentRef.setInput('selectedDurationHours', INTERMITTENT_HOURS);
        fixture.detectChanges();

        const detailsRequested = vi.fn();
        fixture.componentInstance['sessionDetailsRequested'].subscribe(detailsRequested);
        (fixture.nativeElement as HTMLElement).querySelectorAll<HTMLButtonElement>('.fasting-redesign__history-row')[1].click();

        const text = (fixture.nativeElement as HTMLElement).textContent;
        expect(text).toContain('Active session note');
        expect(text).toContain('Active wellbeing note');
        expect(text).not.toContain('Historical session note');
        expect(text).not.toContain('Historical wellbeing note');
        expect(detailsRequested).toHaveBeenCalledWith(expect.objectContaining({ id: 'history-with-notes' }));
    });

    it('presents restored alerts and personal insights in the redesigned layout', async () => {
        await TestBed.configureTestingModule({
            imports: [FastingRedesignPreviewComponent],
            providers: [provideTranslateTesting()],
        }).compileComponents();

        const fixture = TestBed.createComponent(FastingRedesignPreviewComponent);
        fixture.componentRef.setInput('session', createSession());
        fixture.componentRef.setInput('stats', {
            totalCompleted: 3,
            currentStreak: 2,
            averageDurationHours: 14,
            completionRateLast30Days: 75,
            checkInRateLast30Days: 50,
            lastCheckInAtUtc: '2026-08-11T16:00:00Z',
            topSymptom: 'headache',
        });
        fixture.componentRef.setInput('history', []);
        fixture.componentRef.setInput('elapsedFormatted', '01:00:00');
        fixture.componentRef.setInput('remainingFormatted', '15:00:00');
        fixture.componentRef.setInput('progressPercent', TEST_PROGRESS_PERCENT);
        fixture.componentRef.setInput('now', new Date('2026-08-11T16:00:00Z'));
        fixture.componentRef.setInput('selectedDurationHours', INTERMITTENT_HOURS);
        fixture.componentRef.setInput('alerts', [
            {
                message: { id: 'alert-1', titleKey: 'alert', bodyKey: 'body', tone: 'warning', bodyParams: null },
                severity: 'warning',
                title: 'Check how you feel',
                body: 'Your latest check-in deserves attention.',
            },
        ]);
        fixture.componentRef.setInput('insights', [
            {
                message: { id: 'insight-1', titleKey: 'insight', bodyKey: 'body', tone: 'positive', bodyParams: null },
                severity: 'success',
                title: 'A stable pattern',
                body: 'Your recent sessions are consistent.',
            },
        ]);
        fixture.detectChanges();

        const dismissRequested = vi.fn();
        fixture.componentInstance['alertDismissRequested'].subscribe(dismissRequested);
        const alertButtons = (fixture.nativeElement as HTMLElement).querySelectorAll<HTMLButtonElement>('.fasting-redesign__alert button');
        alertButtons[1].click();

        const text = (fixture.nativeElement as HTMLElement).textContent;
        expect(text).toContain('Check how you feel');
        expect(text).toContain('A stable pattern');
        expect(text).toContain('75%');
        expect(dismissRequested).toHaveBeenCalledWith('alert-1');
    });
});

function createSession(overrides: Partial<FastingSession> = {}): FastingSession {
    return {
        id: 'session-1',
        startedAtUtc: '2026-08-11T15:00:00Z',
        endedAtUtc: null,
        initialPlannedDurationHours: 16,
        addedDurationHours: 0,
        plannedDurationHours: 16,
        protocol: 'Fast16Eat8',
        planType: 'Intermittent',
        occurrenceKind: 'FastingWindow',
        cyclicFastDays: null,
        cyclicEatDays: null,
        cyclicEatDayFastHours: null,
        cyclicEatDayEatingWindowHours: null,
        cyclicPhaseDayNumber: null,
        cyclicPhaseDayTotal: null,
        isCompleted: false,
        status: 'Active',
        notes: null,
        checkInAtUtc: null,
        hungerLevel: null,
        energyLevel: null,
        moodLevel: null,
        symptoms: [],
        checkInNotes: null,
        checkIns: [],
        ...overrides,
    };
}
