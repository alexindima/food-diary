import { DatePipe, DecimalPipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, computed, DestroyRef, inject, input, output, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button';
import { FdUiIconComponent } from 'fd-ui-kit/icon/fd-ui-icon';
import { FdUiProgressRingComponent } from 'fd-ui-kit/progress-ring/fd-ui-progress-ring';

import { HOURS_PER_DAY, MS_PER_HOUR } from '../../../../shared/lib/time.constants';
import { buildFastingTimerCardComputedState } from '../../lib/fasting-timer-card-state';
import type { FastingSession, FastingStats } from '../../models/fasting.data';
import type { FastingMessageViewModel } from '../../pages/fasting-page-lib/fasting-page.types';

type RhythmDay = {
    dayKey: string;
    durationLabel: string;
    progress: number;
    completed: boolean;
};

type FastingFlowView = {
    key: string;
    params: Record<string, number>;
};

const COMPLETE_PROGRESS = 100;
const PARTIAL_PROGRESS = 55;
const GOOD_WELLBEING_LEVEL = 4;
const RECENT_SESSIONS_LIMIT = 3;
const RHYTHM_DAY_COUNT = 7;
const DEFAULT_CYCLIC_FAST_HOURS = 16;

@Component({
    selector: 'fd-fasting-redesign-preview',
    imports: [DatePipe, DecimalPipe, TranslatePipe, FdUiButtonComponent, FdUiIconComponent, FdUiProgressRingComponent],
    templateUrl: './fasting-redesign-preview.html',
    styleUrl: './fasting-redesign-preview.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class FastingRedesignPreviewComponent {
    private readonly destroyRef = inject(DestroyRef);
    private readonly translateService = inject(TranslateService);
    private readonly languageVersion = signal(0);

    public readonly session = input.required<FastingSession | null>();
    public readonly stats = input.required<FastingStats | null>();
    public readonly history = input.required<FastingSession[]>();
    public readonly elapsedFormatted = input.required<string>();
    public readonly remainingFormatted = input.required<string>();
    public readonly progressPercent = input.required<number>();
    public readonly now = input.required<Date>();
    public readonly selectedDurationHours = input.required<number>();
    public readonly isStarting = input(false);
    public readonly alerts = input<readonly FastingMessageViewModel[]>([]);
    public readonly insights = input<readonly FastingMessageViewModel[]>([]);
    public readonly startRequested = output();
    public readonly checkInRequested = output();
    public readonly manageRequested = output();
    public readonly protocolSettingsRequested = output();
    public readonly historyRequested = output();
    public readonly sessionDetailsRequested = output<FastingSession>();
    public readonly alertDismissRequested = output<string>();

    protected readonly isActive = computed(() => this.session()?.endedAtUtc === null);
    protected readonly progress = computed(() => Math.min(COMPLETE_PROGRESS, Math.max(0, this.progressPercent())));
    protected readonly primaryAlert = computed(() => this.alerts()[0] ?? null);
    protected readonly personalSummary = computed(() => {
        const stats = this.stats();
        return (
            stats !== null &&
            (stats.completionRateLast30Days > 0 ||
                stats.checkInRateLast30Days > 0 ||
                stats.lastCheckInAtUtc !== null ||
                stats.topSymptom !== null)
        );
    });
    protected readonly topSymptomLabelKey = computed(() => {
        const symptom = this.stats()?.topSymptom;
        return symptom === null || symptom === undefined
            ? 'FASTING.PERSONAL_SUMMARY.NO_SYMPTOM'
            : `FASTING.CHECK_IN.SYMPTOMS.${symptom.toUpperCase()}`;
    });
    protected readonly stageView = computed(() => {
        const session = this.session();
        if (session?.endedAtUtc !== null || this.isEatingPhase()) {
            return null;
        }

        return buildFastingTimerCardComputedState({
            session,
            elapsedMs: Math.max(0, this.now().getTime() - new Date(session.startedAtUtc).getTime()),
            translate: (key, params) => this.translateService.instant(key, params),
        }).stage;
    });
    protected readonly managementLabelKey = computed(() => {
        switch (this.session()?.planType) {
            case 'Cyclic': {
                return 'FASTING.STOP_CYCLE';
            }
            case 'Extended': {
                return 'FASTING.INTERRUPT_FAST';
            }
            case 'Intermittent':
            case undefined: {
                return 'FASTING.END_FAST';
            }
        }
    });
    protected readonly phaseLabelKey = computed(() => {
        const session = this.session();
        if (session?.planType === 'Extended') {
            return 'FASTING.EXTENDED_TYPE';
        }

        return this.isEatingPhase() ? 'FASTING.EATING_WINDOW' : 'FASTING.FASTING_WINDOW';
    });
    protected readonly nextPhaseLabelKey = computed<string | null>(() => {
        const session = this.session();
        if (session?.planType === 'Extended') {
            return null;
        }

        return this.isEatingPhase() ? 'FASTING.FASTING_WINDOW' : 'FASTING.EATING_WINDOW';
    });
    protected readonly flowView = computed<FastingFlowView>((): FastingFlowView => {
        const session = this.session();
        if (session?.planType === 'Cyclic') {
            return {
                key: 'FASTING.REDESIGN.CYCLIC_FLOW',
                params: {
                    fastDays: session.cyclicFastDays ?? 1,
                    eatDays: session.cyclicEatDays ?? 1,
                },
            };
        }

        const fastHours = session?.plannedDurationHours ?? this.selectedDurationHours();
        if (session?.planType === 'Extended') {
            return { key: 'FASTING.REDESIGN.EXTENDED_FLOW', params: { hours: fastHours } };
        }

        return {
            key: 'FASTING.REDESIGN.INTERMITTENT_FLOW',
            params: { fastHours, eatHours: Math.max(0, HOURS_PER_DAY - fastHours) },
        };
    });
    protected readonly protocolLabel = computed(() => {
        this.languageVersion();
        const session = this.session();
        if (session?.planType === 'Cyclic') {
            return `${session.cyclicFastDays ?? 1}:${session.cyclicEatDays ?? 1}`;
        }

        const duration = session?.plannedDurationHours ?? this.selectedDurationHours();
        return duration < HOURS_PER_DAY
            ? `${duration}:${HOURS_PER_DAY - duration}`
            : `${duration} ${this.translateService.instant('FASTING.HOURS')}`;
    });
    protected readonly targetAt = computed(() => {
        const session = this.session();
        if (session === null) {
            return null;
        }

        return new Date(new Date(session.startedAtUtc).getTime() + session.plannedDurationHours * MS_PER_HOUR);
    });
    protected readonly wellbeingKey = computed(() => {
        const session = this.session();
        if (session === null) {
            return 'FASTING.REDESIGN.WELLBEING_EMPTY';
        }

        const checkInScores = session.checkIns.flatMap(checkIn => [checkIn.hungerLevel, checkIn.energyLevel, checkIn.moodLevel]);
        if (
            checkInScores.length === 0 &&
            session.checkInAtUtc !== null &&
            session.hungerLevel !== null &&
            session.energyLevel !== null &&
            session.moodLevel !== null
        ) {
            checkInScores.push(session.hungerLevel, session.energyLevel, session.moodLevel);
        }
        if (checkInScores.length === 0) {
            return 'FASTING.REDESIGN.WELLBEING_EMPTY';
        }

        const averageScore = checkInScores.reduce((sum, score) => sum + score, 0) / checkInScores.length;
        return averageScore >= GOOD_WELLBEING_LEVEL ? 'FASTING.REDESIGN.WELLBEING_GOOD' : 'FASTING.REDESIGN.WELLBEING_NEUTRAL';
    });
    protected readonly recentSessions = computed(() => {
        const currentSession = this.session();
        const history = this.history();
        if (currentSession?.endedAtUtc === null) {
            return [currentSession, ...history.filter(item => item.id !== currentSession.id)].slice(0, RECENT_SESSIONS_LIMIT);
        }

        return history.slice(0, RECENT_SESSIONS_LIMIT);
    });
    protected readonly currentLocale = computed(() => {
        this.languageVersion();
        return this.translateService.currentLang() ?? 'en';
    });
    protected readonly rhythmDays = computed<RhythmDay[]>(() => {
        this.languageVersion();
        return buildRhythmDays(this.history(), this.now(), (key, params) => this.translateService.instant(key, params));
    });

    public constructor() {
        this.translateService.onLangChange.pipe(takeUntilDestroyed(this.destroyRef)).subscribe(() => {
            this.languageVersion.update(version => version + 1);
        });
    }

    protected sessionDurationHours(session: FastingSession): number {
        const end = session.endedAtUtc === null ? new Date() : new Date(session.endedAtUtc);
        return Math.max(0, (end.getTime() - new Date(session.startedAtUtc).getTime()) / MS_PER_HOUR);
    }

    protected historyTypeLabelKey(session: FastingSession): string {
        switch (session.planType) {
            case 'Cyclic': {
                return 'FASTING.CYCLIC_TYPE';
            }
            case 'Extended': {
                return 'FASTING.EXTENDED_TYPE';
            }
            case 'Intermittent': {
                return 'FASTING.INTERMITTENT_TYPE';
            }
        }
    }

    protected historyProtocolLabel(session: FastingSession): string {
        this.languageVersion();
        if (session.planType === 'Cyclic') {
            const cycle = `${session.cyclicFastDays ?? 1}:${session.cyclicEatDays ?? 1}`;
            const eatingWindow = `${session.cyclicEatDayFastHours ?? DEFAULT_CYCLIC_FAST_HOURS}:${
                session.cyclicEatDayEatingWindowHours ?? HOURS_PER_DAY - DEFAULT_CYCLIC_FAST_HOURS
            }`;
            return `${cycle} · ${eatingWindow}`;
        }

        if (session.planType === 'Extended') {
            return `${session.plannedDurationHours} ${this.translateService.instant('FASTING.HOURS')}`;
        }

        return `${session.plannedDurationHours}:${Math.max(0, HOURS_PER_DAY - session.plannedDurationHours)}`;
    }

    protected insightIcon(message: FastingMessageViewModel): string {
        switch (message.message.tone) {
            case 'positive': {
                return 'trending_up';
            }
            case 'warning': {
                return 'warning_amber';
            }
            case 'neutral': {
                return 'lightbulb_outline';
            }
        }
    }

    private isEatingPhase(): boolean {
        const occurrenceKind = this.session()?.occurrenceKind;
        return occurrenceKind === 'EatingWindow' || occurrenceKind === 'EatDay';
    }
}

function buildRhythmDays(
    history: FastingSession[],
    now: Date,
    translate: (key: string, params?: Record<string, number>) => string,
): RhythmDay[] {
    const weekStart = startOfLocalWeek(now);
    const weekEnd = addLocalDays(weekStart, RHYTHM_DAY_COUNT);
    const sessionsByDay = new Map<number, FastingSession[]>();

    for (const session of history) {
        if (session.status === 'Active') {
            continue;
        }

        const startedAt = new Date(session.startedAtUtc);
        if (!Number.isFinite(startedAt.getTime()) || startedAt < weekStart || startedAt >= weekEnd) {
            continue;
        }

        const dayKey = startOfLocalDay(startedAt).getTime();
        const sessions = sessionsByDay.get(dayKey) ?? [];
        sessions.push(session);
        sessionsByDay.set(dayKey, sessions);
    }

    return Array.from({ length: RHYTHM_DAY_COUNT }, (_, index) => {
        const daySessions = sessionsByDay.get(addLocalDays(weekStart, index).getTime()) ?? [];
        return buildRhythmDay(index, daySessions, translate);
    });
}

function buildRhythmDay(
    index: number,
    sessions: FastingSession[],
    translate: (key: string, params?: Record<string, number>) => string,
): RhythmDay {
    if (sessions.length === 0) {
        return {
            dayKey: `FASTING.REDESIGN.DAY_${index + 1}`,
            durationLabel: '-',
            progress: 0,
            completed: false,
        };
    }

    const completedCount = sessions.filter(session => session.status === 'Completed').length;
    const progress = completedCount === 0 ? PARTIAL_PROGRESS : (completedCount / sessions.length) * COMPLETE_PROGRESS;
    const durationLabel =
        sessions.length === 1
            ? `${sessions[0].plannedDurationHours}:${Math.max(0, HOURS_PER_DAY - sessions[0].plannedDurationHours)}`
            : translate('FASTING.REDESIGN.SESSIONS_COUNT', { count: sessions.length });

    return {
        dayKey: `FASTING.REDESIGN.DAY_${index + 1}`,
        durationLabel,
        progress,
        completed: completedCount === sessions.length,
    };
}

function startOfLocalWeek(date: Date): Date {
    const day = startOfLocalDay(date);
    const mondayOffset = (day.getDay() + RHYTHM_DAY_COUNT - 1) % RHYTHM_DAY_COUNT;
    return addLocalDays(day, -mondayOffset);
}

function startOfLocalDay(date: Date): Date {
    return new Date(date.getFullYear(), date.getMonth(), date.getDate());
}

function addLocalDays(date: Date, days: number): Date {
    return new Date(date.getFullYear(), date.getMonth(), date.getDate() + days);
}
