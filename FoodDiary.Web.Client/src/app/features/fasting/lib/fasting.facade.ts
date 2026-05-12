import { computed, DestroyRef, inject, Injectable, signal, type WritableSignal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import type { Observable } from 'rxjs';

import { FrontendObservabilityService } from '../../../services/frontend-observability.service';
import { UserService } from '../../../shared/api/user.service';
import { resolveFastingReminderPresetId } from '../../../shared/lib/fasting-reminder-presets';
import { PERCENT_MULTIPLIER } from '../../../shared/lib/nutrition.constants';
import { runTrackedRequest } from '../../../shared/lib/run-tracked-request';
import { HOURS_PER_DAY, MS_PER_HOUR, MS_PER_SECOND, SECONDS_PER_MINUTE } from '../../../shared/lib/time.constants';
import { FastingService } from '../api/fasting.service';
import {
    FASTING_PROTOCOLS,
    type FastingInsights,
    type FastingMessage,
    type FastingMode,
    type FastingOverview,
    type FastingPlanType,
    type FastingProtocol,
    type FastingSession,
    type FastingStats,
} from '../models/fasting.data';
import { FastingPromptStateStore } from './fasting-prompt-state.store';

const PROMPT_SNOOZE_HOURS = 4;
const HISTORY_PAGE_SIZE = 10;
const DEFAULT_INTERMITTENT_FAST_HOURS = 16;
const DEFAULT_EXTEND_HOURS = 24;
const DEFAULT_REDUCE_HOURS = 4;
const DEFAULT_CHECK_IN_LEVEL = 3;
const MIN_FASTING_HOURS = 1;
const MAX_FASTING_HOURS = 168;
const MAX_INTERMITTENT_FAST_HOURS = 23;
const MAX_CYCLIC_DAYS = 30;
const MAX_CHECK_IN_LEVEL = 5;
const SECONDS_PER_HOUR = 3600;
const DURATION_PART_LENGTH = 2;
const DURATION_PART_PAD = '0';
const DURATION_ROUNDING_FACTOR = 10;
const HISTORY_FROM_MONTH_OFFSET = 1;
const HISTORY_TO_MONTH_OFFSET = 2;
const DEFAULT_FIRST_REMINDER_HOURS = 12;
const DEFAULT_FOLLOW_UP_REMINDER_HOURS = 20;

type FastingPromptState = {
    dismissed?: boolean;
    snoozedUntilUtc?: string;
};

@Injectable({ providedIn: 'root' })
export class FastingFacade {
    private readonly fastingService = inject(FastingService);
    private readonly frontendObservability = inject(FrontendObservabilityService);
    private readonly promptStateStore = inject(FastingPromptStateStore);
    private readonly userService = inject(UserService);
    private readonly destroyRef = inject(DestroyRef);
    private timerInterval: ReturnType<typeof setInterval> | null = null;

    public readonly isLoading = signal(false);
    public readonly isStarting = signal(false);
    public readonly isEnding = signal(false);
    public readonly isExtending = signal(false);
    public readonly isReducing = signal(false);
    public readonly isUpdatingCycle = signal(false);
    public readonly isSavingCheckIn = signal(false);
    public readonly currentSession = signal<FastingSession | null>(null);
    public readonly stats = signal<FastingStats | null>(null);
    public readonly history = signal<FastingSession[]>([]);
    public readonly historyPage = signal(1);
    public readonly historyTotalPages = signal(0);
    public readonly isLoadingMoreHistory = signal(false);
    public readonly insightsData = signal<FastingInsights>({ alerts: [], insights: [] });
    public readonly checkInSavedVersion = signal(0);
    public readonly selectedMode = signal<FastingMode>('intermittent');
    public readonly selectedProtocol = signal<FastingProtocol>('F16_8');
    public readonly customHours = signal(DEFAULT_INTERMITTENT_FAST_HOURS);
    public readonly customIntermittentFastHours = signal(DEFAULT_INTERMITTENT_FAST_HOURS);
    public readonly cyclicEatDayProtocol = signal<FastingProtocol>('F16_8');
    public readonly cyclicFastDays = signal(1);
    public readonly cyclicEatDays = signal(1);
    public readonly cyclicUsesCustomPreset = signal(false);
    public readonly cyclicEatDayFastHours = signal(DEFAULT_INTERMITTENT_FAST_HOURS);
    public readonly extendHours = signal(DEFAULT_EXTEND_HOURS);
    public readonly reduceHours = signal(DEFAULT_REDUCE_HOURS);
    public readonly hungerLevel = signal(DEFAULT_CHECK_IN_LEVEL);
    public readonly energyLevel = signal(DEFAULT_CHECK_IN_LEVEL);
    public readonly moodLevel = signal(DEFAULT_CHECK_IN_LEVEL);
    public readonly selectedSymptoms = signal<string[]>([]);
    public readonly checkInNotes = signal('');
    public readonly now = signal(new Date());
    public readonly promptState = signal<Partial<Record<string, FastingPromptState>>>(this.promptStateStore.read());

    public readonly isActive = computed(() => {
        const session = this.currentSession();
        return session !== null && session.endedAtUtc === null;
    });

    public readonly plannedDurationHours = computed(() => {
        const protocol = this.selectedProtocol();
        if (protocol === 'CustomIntermittent') {
            return this.customIntermittentFastHours();
        }
        if (protocol === 'Custom') {
            return this.customHours();
        }
        return FASTING_PROTOCOLS.find(p => p.value === protocol)?.hours ?? DEFAULT_INTERMITTENT_FAST_HOURS;
    });

    public readonly elapsedMs = computed(() => {
        const session = this.currentSession();
        if (session === null) {
            return 0;
        }
        const start = new Date(session.startedAtUtc).getTime();
        const end = session.endedAtUtc !== null ? new Date(session.endedAtUtc).getTime() : this.now().getTime();
        return Math.max(0, end - start);
    });

    public readonly totalMs = computed(() => {
        const session = this.currentSession();
        if (session === null) {
            return this.plannedDurationHours() * MS_PER_HOUR;
        }
        return session.plannedDurationHours * MS_PER_HOUR;
    });

    public readonly progressPercent = computed(() => {
        const total = this.totalMs();
        if (total <= 0) {
            return 0;
        }
        return Math.min((this.elapsedMs() / total) * PERCENT_MULTIPLIER, PERCENT_MULTIPLIER);
    });

    public readonly elapsedFormatted = computed(() => this.formatDuration(this.elapsedMs()));

    public readonly remainingFormatted = computed(() => {
        const remaining = Math.max(0, this.totalMs() - this.elapsedMs());
        return this.formatDuration(remaining);
    });

    public readonly isOvertime = computed(() => this.elapsedMs() > this.totalMs());
    public readonly canExtendActiveSession = computed(() => {
        const session = this.currentSession();
        return session !== null && session.endedAtUtc === null && session.planType === 'Extended';
    });

    public initialize(): void {
        this.trackRequest(this.isLoading, this.fastingService.getOverview(), overview => {
            this.applyOverview(overview);
        });
    }

    public loadMoreHistory(): void {
        if (this.isLoadingMoreHistory() || this.historyPage() >= this.historyTotalPages()) {
            return;
        }

        const range = this.getHistoryRange();
        this.trackRequest(
            this.isLoadingMoreHistory,
            this.fastingService.getHistory({
                from: range.from,
                to: range.to,
                page: this.historyPage() + 1,
                limit: HISTORY_PAGE_SIZE,
            }),
            history => {
                this.history.update(current => [...current, ...history.data]);
                this.historyPage.set(history.page);
                this.historyTotalPages.set(history.totalPages);
            },
        );
    }

    public startFasting(): void {
        const payload = this.buildStartPayload();
        this.trackRequest(this.isStarting, this.fastingService.start(payload), session => {
            this.frontendObservability.recordFastingLifecycleEvent('session.started', {
                sessionId: session.id,
                protocol: session.protocol,
                planType: session.planType,
                plannedDurationHours: session.plannedDurationHours,
                occurrenceKind: session.occurrenceKind,
                ...this.getReminderTelemetryDetails(),
            });
            this.refreshOverview();
        });
    }

    public endFasting(): void {
        this.trackRequest(this.isEnding, this.fastingService.end(), session => {
            if (session.endedAtUtc !== null) {
                this.stopTimer();
                this.frontendObservability.recordFastingLifecycleEvent('session.completed', {
                    sessionId: session.id,
                    protocol: session.protocol,
                    planType: session.planType,
                    status: session.status,
                    plannedDurationHours: session.plannedDurationHours,
                    actualDurationHours: this.getSessionDurationHours(session),
                    hadCheckIn: session.checkInAtUtc !== null,
                    ...this.getReminderTelemetryDetails(),
                });
                this.clearPromptStateForSession(session.id);
                this.resetDraftState();
                this.refreshOverview();
            } else {
                this.refreshOverview();
            }
        });
    }

    public selectProtocol(protocol: FastingProtocol): void {
        this.selectedProtocol.set(protocol);
    }

    public selectMode(mode: FastingMode): void {
        this.selectedMode.set(mode);

        if (mode === 'intermittent' && !this.isSelectedProtocolInCategory('intermittent')) {
            this.selectedProtocol.set('F16_8');
            return;
        }

        if (mode === 'extended' && !this.isSelectedProtocolInCategory('extended')) {
            this.selectedProtocol.set('F24_0');
        }
    }

    public setCustomHours(hours: number): void {
        this.customHours.set(this.clampFastingHours(hours));
    }

    public setCustomIntermittentFastHours(hours: number): void {
        this.customIntermittentFastHours.set(this.clampIntermittentFastHours(hours));
    }

    public setCyclicPreset(fastDays: number, eatDays: number): void {
        this.cyclicUsesCustomPreset.set(false);
        this.cyclicFastDays.set(this.clampCyclicDays(fastDays));
        this.cyclicEatDays.set(this.clampCyclicDays(eatDays));
    }

    public selectCustomCyclicPreset(): void {
        this.cyclicUsesCustomPreset.set(true);
    }

    public setCyclicFastDays(days: number): void {
        this.cyclicUsesCustomPreset.set(true);
        this.cyclicFastDays.set(this.clampCyclicDays(days));
    }

    public setCyclicEatDays(days: number): void {
        this.cyclicUsesCustomPreset.set(true);
        this.cyclicEatDays.set(this.clampCyclicDays(days));
    }

    public selectCyclicEatDayProtocol(protocol: FastingProtocol): void {
        this.cyclicEatDayProtocol.set(protocol);

        if (protocol === 'CustomIntermittent') {
            return;
        }

        const preset = FASTING_PROTOCOLS.find(item => item.value === protocol && item.category === 'intermittent');
        if (preset !== undefined) {
            this.cyclicEatDayFastHours.set(this.clampIntermittentFastHours(preset.hours));
        }
    }

    public setCyclicEatDayFastHours(hours: number): void {
        this.cyclicEatDayProtocol.set('CustomIntermittent');
        this.cyclicEatDayFastHours.set(this.clampIntermittentFastHours(hours));
    }

    public setExtendHours(hours: number): void {
        this.extendHours.set(this.clampFastingHours(hours));
    }

    public setReduceHours(hours: number): void {
        this.reduceHours.set(this.clampFastingHours(hours));
    }

    public setHungerLevel(level: number): void {
        this.hungerLevel.set(this.clampCheckInLevel(level));
    }

    public setEnergyLevel(level: number): void {
        this.energyLevel.set(this.clampCheckInLevel(level));
    }

    public setMoodLevel(level: number): void {
        this.moodLevel.set(this.clampCheckInLevel(level));
    }

    public toggleSymptom(symptom: string): void {
        this.selectedSymptoms.update(current =>
            current.includes(symptom) ? current.filter(value => value !== symptom) : [...current, symptom],
        );
    }

    public setCheckInNotes(value: string): void {
        this.checkInNotes.set(value);
    }

    public resetCheckInDraft(): void {
        this.syncCheckInFromSession(this.currentSession());
    }

    public extendByHours(hours: number): void {
        const additionalHours = this.clampFastingHours(hours);
        runTrackedRequest(this.destroyRef, this.isExtending, this.fastingService.extend({ additionalHours }), {
            next: session => {
                this.applyCurrentSessionUpdate(session);
            },
        });
    }

    public reduceTargetByHours(hours: number): void {
        const reducedHours = this.clampFastingHours(hours);
        runTrackedRequest(this.destroyRef, this.isReducing, this.fastingService.reduceTarget({ reducedHours }), {
            next: session => {
                if (session.endedAtUtc !== null) {
                    this.resetDraftState();
                    this.applyCompletedSessionUpdate(session);
                } else {
                    this.applyCurrentSessionUpdate(session);
                }
            },
        });
    }

    public saveCheckIn(): void {
        const session = this.currentSession();
        if (session?.endedAtUtc !== null) {
            return;
        }
        const checkInNotes = this.checkInNotes().trim();

        this.trackRequest(
            this.isSavingCheckIn,
            this.fastingService.updateCheckIn({
                hungerLevel: this.hungerLevel(),
                energyLevel: this.energyLevel(),
                moodLevel: this.moodLevel(),
                symptoms: this.selectedSymptoms(),
                checkInNotes: checkInNotes.length > 0 ? checkInNotes : null,
            }),
            updated => {
                this.checkInSavedVersion.update(version => version + 1);
                this.frontendObservability.recordFastingLifecycleEvent('check-in.saved', {
                    sessionId: updated.id,
                    protocol: updated.protocol,
                    planType: updated.planType,
                    hungerLevel: updated.hungerLevel,
                    energyLevel: updated.energyLevel,
                    moodLevel: updated.moodLevel,
                    symptomsCount: updated.symptoms.length,
                    hadNotes: updated.checkInNotes !== null && updated.checkInNotes.length > 0,
                    ...this.getReminderTelemetryDetails(),
                });
                this.clearPromptStateForSession(updated.id);
                this.refreshOverview();
            },
        );
    }

    public isPromptVisible(session: FastingSession | null, prompt: FastingMessage | null): boolean {
        if (session === null || prompt === null || session.endedAtUtc !== null) {
            return false;
        }

        const state = this.promptState()[this.getPromptStateKey(session.id, prompt.id)];
        if (state === undefined) {
            return true;
        }

        if (state.dismissed === true) {
            return false;
        }

        if (state.snoozedUntilUtc === undefined || state.snoozedUntilUtc.length === 0) {
            return true;
        }

        return new Date(state.snoozedUntilUtc).getTime() <= this.now().getTime();
    }

    public dismissPrompt(promptId: string): void {
        const session = this.currentSession();
        if (session === null) {
            return;
        }

        this.updatePromptState(this.getPromptStateKey(session.id, promptId), { dismissed: true });
    }

    public snoozePrompt(promptId: string): void {
        const session = this.currentSession();
        if (session === null) {
            return;
        }

        const snoozedUntilUtc = new Date(this.now().getTime() + PROMPT_SNOOZE_HOURS * MS_PER_HOUR).toISOString();
        this.updatePromptState(this.getPromptStateKey(session.id, promptId), { snoozedUntilUtc });
    }

    public skipCyclicDay(): void {
        this.trackRequest(this.isUpdatingCycle, this.fastingService.skipCyclicDay(), () => {
            this.refreshOverview();
        });
    }

    public postponeCyclicDay(): void {
        this.trackRequest(this.isUpdatingCycle, this.fastingService.postponeCyclicDay(), () => {
            this.refreshOverview();
        });
    }

    private startTimer(): void {
        this.stopTimer();
        this.now.set(new Date());
        this.timerInterval = setInterval(() => {
            this.now.set(new Date());
        }, MS_PER_SECOND);
        this.destroyRef.onDestroy(() => {
            this.stopTimer();
        });
    }

    private stopTimer(): void {
        if (this.timerInterval !== null) {
            clearInterval(this.timerInterval);
            this.timerInterval = null;
        }
    }

    private refreshOverview(): void {
        this.fastingService
            .getOverview()
            .pipe(takeUntilDestroyed(this.destroyRef))
            .subscribe(overview => {
                this.applyOverview(overview);
            });
    }

    private applyOverview(overview: FastingOverview): void {
        this.currentSession.set(overview.currentSession);
        this.stats.set(overview.stats);
        this.history.set(overview.history.data);
        this.historyPage.set(overview.history.page);
        this.historyTotalPages.set(overview.history.totalPages);
        this.insightsData.set(overview.insights);
        this.syncCheckInFromSession(overview.currentSession);

        if (overview.currentSession !== null && overview.currentSession.endedAtUtc === null) {
            this.startTimer();
            return;
        }

        this.stopTimer();
    }

    private applyCurrentSessionUpdate(session: FastingSession): void {
        this.currentSession.set(session);
        this.syncCheckInFromSession(session);
        this.upsertHistorySession(session);
        this.startTimer();
    }

    private applyCompletedSessionUpdate(session: FastingSession): void {
        this.stopTimer();
        this.currentSession.set(null);
        this.syncCheckInFromSession(null);
        this.upsertHistorySession(session);
        this.insightsData.update(current => ({ ...current, alerts: [] }));
    }

    private upsertHistorySession(session: FastingSession): void {
        this.history.update(current => {
            const existingIndex = current.findIndex(item => item.id === session.id);
            if (existingIndex < 0) {
                return current;
            }

            const next = [...current];
            next[existingIndex] = session;
            return next;
        });
    }

    private syncCheckInFromSession(session: FastingSession | null): void {
        if (session === null) {
            this.resetCheckInDraftSignals();
            return;
        }

        this.hungerLevel.set(session.hungerLevel ?? DEFAULT_CHECK_IN_LEVEL);
        this.energyLevel.set(session.energyLevel ?? DEFAULT_CHECK_IN_LEVEL);
        this.moodLevel.set(session.moodLevel ?? DEFAULT_CHECK_IN_LEVEL);
        this.selectedSymptoms.set(session.symptoms);
        this.checkInNotes.set(session.checkInNotes ?? '');
    }

    private resetCheckInDraftSignals(): void {
        this.hungerLevel.set(DEFAULT_CHECK_IN_LEVEL);
        this.energyLevel.set(DEFAULT_CHECK_IN_LEVEL);
        this.moodLevel.set(DEFAULT_CHECK_IN_LEVEL);
        this.selectedSymptoms.set([]);
        this.checkInNotes.set('');
    }

    private isSelectedProtocolInCategory(category: 'intermittent' | 'extended'): boolean {
        const protocol = this.selectedProtocol();
        return FASTING_PROTOCOLS.some(item => item.value === protocol && item.category === category);
    }

    private getPromptStateKey(sessionId: string, promptId: string): string {
        return `${sessionId}:${promptId}`;
    }

    private updatePromptState(key: string, value: FastingPromptState): void {
        this.promptState.update(current => {
            const next = { ...current, [key]: { ...current[key], ...value } };
            this.promptStateStore.write(next);
            return next;
        });
    }

    private clearPromptStateForSession(sessionId: string): void {
        this.promptState.update(current => {
            const next = Object.fromEntries(Object.entries(current).filter(([key]) => !key.startsWith(`${sessionId}:`)));
            this.promptStateStore.write(next);
            return next;
        });
    }

    private trackRequest<T>(state: WritableSignal<boolean>, request$: Observable<T>, next: (value: T) => void): void {
        runTrackedRequest(this.destroyRef, state, request$, { next });
    }

    private formatDuration(ms: number): string {
        const totalSeconds = Math.floor(ms / MS_PER_SECOND);
        const hours = Math.floor(totalSeconds / SECONDS_PER_HOUR);
        const minutes = Math.floor((totalSeconds % SECONDS_PER_HOUR) / SECONDS_PER_MINUTE);
        const seconds = totalSeconds % SECONDS_PER_MINUTE;
        return `${this.formatDurationPart(hours)}:${this.formatDurationPart(minutes)}:${this.formatDurationPart(seconds)}`;
    }

    private formatDurationPart(value: number): string {
        return String(value).padStart(DURATION_PART_LENGTH, DURATION_PART_PAD);
    }

    private getSessionDurationHours(session: FastingSession): number {
        const endedAt = session.endedAtUtc;
        if (endedAt === null) {
            return 0;
        }

        const startedAtMs = new Date(session.startedAtUtc).getTime();
        const endedAtMs = new Date(endedAt).getTime();
        if (Number.isNaN(startedAtMs) || Number.isNaN(endedAtMs) || endedAtMs <= startedAtMs) {
            return 0;
        }

        return Math.round(((endedAtMs - startedAtMs) / MS_PER_HOUR) * DURATION_ROUNDING_FACTOR) / DURATION_ROUNDING_FACTOR;
    }

    private getReminderTelemetryDetails(): Record<string, unknown> {
        const user = this.userService.user();
        const firstReminderHours = user?.fastingCheckInReminderHours ?? DEFAULT_FIRST_REMINDER_HOURS;
        const followUpReminderHours = user?.fastingCheckInFollowUpReminderHours ?? DEFAULT_FOLLOW_UP_REMINDER_HOURS;
        const reminderPresetId = resolveFastingReminderPresetId(firstReminderHours, followUpReminderHours);

        return {
            firstReminderHours,
            followUpReminderHours,
            reminderPresetId,
        };
    }

    private clampFastingHours(hours: number): number {
        return Math.max(MIN_FASTING_HOURS, Math.min(MAX_FASTING_HOURS, hours));
    }

    private clampIntermittentFastHours(hours: number): number {
        return Math.max(MIN_FASTING_HOURS, Math.min(MAX_INTERMITTENT_FAST_HOURS, hours));
    }

    private clampCyclicDays(days: number): number {
        return Math.max(MIN_FASTING_HOURS, Math.min(MAX_CYCLIC_DAYS, days));
    }

    private clampCheckInLevel(level: number): number {
        return Math.max(MIN_FASTING_HOURS, Math.min(MAX_CHECK_IN_LEVEL, level));
    }

    private resetDraftState(): void {
        this.selectedMode.set('intermittent');
        this.selectedProtocol.set('F16_8');
        this.customHours.set(DEFAULT_INTERMITTENT_FAST_HOURS);
        this.customIntermittentFastHours.set(DEFAULT_INTERMITTENT_FAST_HOURS);
        this.cyclicEatDayProtocol.set('F16_8');
        this.cyclicFastDays.set(1);
        this.cyclicEatDays.set(1);
        this.cyclicUsesCustomPreset.set(false);
        this.cyclicEatDayFastHours.set(DEFAULT_INTERMITTENT_FAST_HOURS);
        this.extendHours.set(DEFAULT_EXTEND_HOURS);
        this.reduceHours.set(DEFAULT_REDUCE_HOURS);
    }

    private getHistoryRange(): { from: string; to: string } {
        const now = new Date();
        const from = new Date(Date.UTC(now.getUTCFullYear(), now.getUTCMonth() - HISTORY_FROM_MONTH_OFFSET, 1, 0, 0, 0, 0));
        const to = new Date(Date.UTC(now.getUTCFullYear(), now.getUTCMonth() + HISTORY_TO_MONTH_OFFSET, 1, 0, 0, 0, 0) - 1);

        return {
            from: from.toISOString(),
            to: to.toISOString(),
        };
    }

    private buildStartPayload(): {
        protocol?: string;
        planType?: FastingPlanType;
        plannedDurationHours?: number;
        cyclicFastDays?: number;
        cyclicEatDays?: number;
        cyclicEatDayFastHours?: number;
        cyclicEatDayEatingWindowHours?: number;
    } {
        const selectedMode = this.selectedMode();
        if (selectedMode === 'cyclic') {
            return {
                planType: 'Cyclic',
                cyclicFastDays: this.cyclicFastDays(),
                cyclicEatDays: this.cyclicEatDays(),
                cyclicEatDayFastHours: this.cyclicEatDayFastHours(),
                cyclicEatDayEatingWindowHours: HOURS_PER_DAY - this.cyclicEatDayFastHours(),
            };
        }

        return {
            planType: selectedMode === 'intermittent' ? 'Intermittent' : 'Extended',
            protocol: this.selectedProtocol(),
            plannedDurationHours: this.plannedDurationHours(),
        };
    }
}
