import { DestroyRef, Injectable, computed, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { finalize } from 'rxjs';
import { FastingService } from '../api/fasting.service';
import { FrontendObservabilityService } from '../../../services/frontend-observability.service';
import { UserService } from '../../../shared/api/user.service';
import { resolveFastingReminderPresetId } from '../../../shared/lib/fasting-reminder-presets';
import {
    FASTING_PROTOCOLS,
    FastingInsights,
    FastingMessage,
    FastingMode,
    FastingOverview,
    FastingPlanType,
    FastingProtocol,
    FastingSession,
    FastingStats,
} from '../models/fasting.data';

interface FastingPromptState {
    dismissed?: boolean;
    snoozedUntilUtc?: string;
}

@Injectable()
export class FastingFacade {
    private static readonly PromptStorageKey = 'fd_fasting_prompt_state';
    private static readonly PromptSnoozeHours = 4;
    private static readonly HistoryPageSize = 10;

    private readonly fastingService = inject(FastingService);
    private readonly frontendObservability = inject(FrontendObservabilityService);
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
    public readonly customHours = signal(16);
    public readonly customIntermittentFastHours = signal(16);
    public readonly cyclicEatDayProtocol = signal<FastingProtocol>('F16_8');
    public readonly cyclicFastDays = signal(1);
    public readonly cyclicEatDays = signal(1);
    public readonly cyclicUsesCustomPreset = signal(false);
    public readonly cyclicEatDayFastHours = signal(16);
    public readonly extendHours = signal(24);
    public readonly reduceHours = signal(4);
    public readonly hungerLevel = signal(3);
    public readonly energyLevel = signal(3);
    public readonly moodLevel = signal(3);
    public readonly selectedSymptoms = signal<string[]>([]);
    public readonly checkInNotes = signal('');
    public readonly now = signal(new Date());
    public readonly promptState = signal<Record<string, FastingPromptState>>(this.readPromptState());

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
        return FASTING_PROTOCOLS.find(p => p.value === protocol)?.hours ?? 16;
    });

    public readonly elapsedMs = computed(() => {
        const session = this.currentSession();
        if (!session) {
            return 0;
        }
        const start = new Date(session.startedAtUtc).getTime();
        const end = session.endedAtUtc ? new Date(session.endedAtUtc).getTime() : this.now().getTime();
        return Math.max(0, end - start);
    });

    public readonly totalMs = computed(() => {
        const session = this.currentSession();
        if (!session) {
            return this.plannedDurationHours() * 3600_000;
        }
        return session.plannedDurationHours * 3600_000;
    });

    public readonly progressPercent = computed(() => {
        const total = this.totalMs();
        if (total <= 0) {
            return 0;
        }
        return Math.min((this.elapsedMs() / total) * 100, 100);
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
        this.isLoading.set(true);
        this.fastingService
            .getOverview()
            .pipe(
                finalize(() => this.isLoading.set(false)),
                takeUntilDestroyed(this.destroyRef),
            )
            .subscribe(overview => this.applyOverview(overview));
    }

    public loadMoreHistory(): void {
        if (this.isLoadingMoreHistory() || this.historyPage() >= this.historyTotalPages()) {
            return;
        }

        const range = this.getHistoryRange();
        this.isLoadingMoreHistory.set(true);
        this.fastingService
            .getHistory({
                from: range.from,
                to: range.to,
                page: this.historyPage() + 1,
                limit: FastingFacade.HistoryPageSize,
            })
            .pipe(
                finalize(() => this.isLoadingMoreHistory.set(false)),
                takeUntilDestroyed(this.destroyRef),
            )
            .subscribe(history => {
                this.history.update(current => [...current, ...history.data]);
                this.historyPage.set(history.page);
                this.historyTotalPages.set(history.totalPages);
            });
    }

    public startFasting(): void {
        this.isStarting.set(true);
        const payload = this.buildStartPayload();
        this.fastingService
            .start(payload)
            .pipe(
                finalize(() => this.isStarting.set(false)),
                takeUntilDestroyed(this.destroyRef),
            )
            .subscribe(session => {
                this.currentSession.set(session);
                this.syncCheckInFromSession(session);
                this.frontendObservability.recordFastingLifecycleEvent('session.started', {
                    sessionId: session.id,
                    protocol: session.protocol,
                    planType: session.planType,
                    plannedDurationHours: session.plannedDurationHours,
                    occurrenceKind: session.occurrenceKind,
                    ...this.getReminderTelemetryDetails(),
                });
                this.startTimer();
                this.refreshOverview();
            });
    }

    public endFasting(): void {
        this.isEnding.set(true);
        this.fastingService
            .end()
            .pipe(
                finalize(() => this.isEnding.set(false)),
                takeUntilDestroyed(this.destroyRef),
            )
            .subscribe(session => {
                if (session.endedAtUtc) {
                    this.stopTimer();
                    this.frontendObservability.recordFastingLifecycleEvent('session.completed', {
                        sessionId: session.id,
                        protocol: session.protocol,
                        planType: session.planType,
                        status: session.status,
                        plannedDurationHours: session.plannedDurationHours,
                        actualDurationHours: this.getSessionDurationHours(session),
                        hadCheckIn: !!session.checkInAtUtc,
                        ...this.getReminderTelemetryDetails(),
                    });
                    this.clearPromptStateForSession(session.id);
                    this.currentSession.set(null);
                    this.resetDraftState();
                    this.syncCheckInFromSession(null);
                    this.refreshOverview();
                } else {
                    this.currentSession.set(session);
                    this.syncCheckInFromSession(session);
                    this.startTimer();
                    this.refreshOverview();
                }
            });
    }

    public selectProtocol(protocol: FastingProtocol): void {
        this.selectedProtocol.set(protocol);
    }

    public selectMode(mode: FastingMode): void {
        this.selectedMode.set(mode);

        if (mode === 'extended') {
            this.selectedProtocol.set('F24_0');
        }
    }

    public setCustomHours(hours: number): void {
        this.customHours.set(Math.max(1, Math.min(168, hours)));
    }

    public setCustomIntermittentFastHours(hours: number): void {
        this.customIntermittentFastHours.set(Math.max(1, Math.min(23, hours)));
    }

    public setCyclicPreset(fastDays: number, eatDays: number): void {
        this.cyclicUsesCustomPreset.set(false);
        this.cyclicFastDays.set(Math.max(1, Math.min(30, fastDays)));
        this.cyclicEatDays.set(Math.max(1, Math.min(30, eatDays)));
    }

    public selectCustomCyclicPreset(): void {
        this.cyclicUsesCustomPreset.set(true);
    }

    public setCyclicFastDays(days: number): void {
        this.cyclicUsesCustomPreset.set(true);
        this.cyclicFastDays.set(Math.max(1, Math.min(30, days)));
    }

    public setCyclicEatDays(days: number): void {
        this.cyclicUsesCustomPreset.set(true);
        this.cyclicEatDays.set(Math.max(1, Math.min(30, days)));
    }

    public selectCyclicEatDayProtocol(protocol: FastingProtocol): void {
        this.cyclicEatDayProtocol.set(protocol);

        if (protocol === 'CustomIntermittent') {
            return;
        }

        const preset = FASTING_PROTOCOLS.find(item => item.value === protocol && item.category === 'intermittent');
        if (preset) {
            this.cyclicEatDayFastHours.set(Math.max(1, Math.min(23, preset.hours)));
        }
    }

    public setCyclicEatDayFastHours(hours: number): void {
        this.cyclicEatDayProtocol.set('CustomIntermittent');
        this.cyclicEatDayFastHours.set(Math.max(1, Math.min(23, hours)));
    }

    public setExtendHours(hours: number): void {
        this.extendHours.set(Math.max(1, Math.min(168, hours)));
    }

    public setReduceHours(hours: number): void {
        this.reduceHours.set(Math.max(1, Math.min(168, hours)));
    }

    public setHungerLevel(level: number): void {
        this.hungerLevel.set(Math.max(1, Math.min(5, level)));
    }

    public setEnergyLevel(level: number): void {
        this.energyLevel.set(Math.max(1, Math.min(5, level)));
    }

    public setMoodLevel(level: number): void {
        this.moodLevel.set(Math.max(1, Math.min(5, level)));
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
        const additionalHours = Math.max(1, Math.min(168, hours));
        this.isExtending.set(true);
        this.fastingService
            .extend({ additionalHours })
            .pipe(
                finalize(() => this.isExtending.set(false)),
                takeUntilDestroyed(this.destroyRef),
            )
            .subscribe(session => {
                this.currentSession.set(session);
                this.syncCheckInFromSession(session);
                this.refreshOverview();
            });
    }

    public reduceTargetByHours(hours: number): void {
        const reducedHours = Math.max(1, Math.min(168, hours));
        this.isReducing.set(true);
        this.fastingService
            .reduceTarget({ reducedHours })
            .pipe(
                finalize(() => this.isReducing.set(false)),
                takeUntilDestroyed(this.destroyRef),
            )
            .subscribe(session => {
                if (session.endedAtUtc) {
                    this.stopTimer();
                    this.currentSession.set(null);
                    this.resetDraftState();
                    this.syncCheckInFromSession(null);
                } else {
                    this.currentSession.set(session);
                    this.syncCheckInFromSession(session);
                    this.startTimer();
                }

                this.refreshOverview();
            });
    }

    public saveCheckIn(): void {
        const session = this.currentSession();
        if (!session || session.endedAtUtc) {
            return;
        }

        this.isSavingCheckIn.set(true);
        this.fastingService
            .updateCheckIn({
                hungerLevel: this.hungerLevel(),
                energyLevel: this.energyLevel(),
                moodLevel: this.moodLevel(),
                symptoms: this.selectedSymptoms(),
                checkInNotes: this.checkInNotes().trim() || null,
            })
            .pipe(
                finalize(() => this.isSavingCheckIn.set(false)),
                takeUntilDestroyed(this.destroyRef),
            )
            .subscribe(updated => {
                this.currentSession.set(updated);
                this.syncCheckInFromSession(updated);
                this.checkInSavedVersion.update(version => version + 1);
                this.frontendObservability.recordFastingLifecycleEvent('check-in.saved', {
                    sessionId: updated.id,
                    protocol: updated.protocol,
                    planType: updated.planType,
                    hungerLevel: updated.hungerLevel,
                    energyLevel: updated.energyLevel,
                    moodLevel: updated.moodLevel,
                    symptomsCount: updated.symptoms.length,
                    hadNotes: !!updated.checkInNotes,
                    ...this.getReminderTelemetryDetails(),
                });
                this.clearPromptStateForSession(updated.id);
                this.refreshOverview();
            });
    }

    public isPromptVisible(session: FastingSession | null, prompt: FastingMessage | null): boolean {
        if (!session || !prompt || session.endedAtUtc) {
            return false;
        }

        const state = this.promptState()[this.getPromptStateKey(session.id, prompt.id)];
        if (!state) {
            return true;
        }

        if (state.dismissed) {
            return false;
        }

        if (!state.snoozedUntilUtc) {
            return true;
        }

        return new Date(state.snoozedUntilUtc).getTime() <= this.now().getTime();
    }

    public dismissPrompt(promptId: string): void {
        const session = this.currentSession();
        if (!session) {
            return;
        }

        this.updatePromptState(this.getPromptStateKey(session.id, promptId), { dismissed: true });
    }

    public snoozePrompt(promptId: string): void {
        const session = this.currentSession();
        if (!session) {
            return;
        }

        const snoozedUntilUtc = new Date(this.now().getTime() + FastingFacade.PromptSnoozeHours * 3_600_000).toISOString();
        this.updatePromptState(this.getPromptStateKey(session.id, promptId), { snoozedUntilUtc });
    }

    public skipCyclicDay(): void {
        this.isUpdatingCycle.set(true);
        this.fastingService
            .skipCyclicDay()
            .pipe(
                finalize(() => this.isUpdatingCycle.set(false)),
                takeUntilDestroyed(this.destroyRef),
            )
            .subscribe(session => {
                this.currentSession.set(session);
                this.syncCheckInFromSession(session);
                this.startTimer();
                this.refreshOverview();
            });
    }

    public postponeCyclicDay(): void {
        this.isUpdatingCycle.set(true);
        this.fastingService
            .postponeCyclicDay()
            .pipe(
                finalize(() => this.isUpdatingCycle.set(false)),
                takeUntilDestroyed(this.destroyRef),
            )
            .subscribe(session => {
                this.currentSession.set(session);
                this.syncCheckInFromSession(session);
                this.startTimer();
                this.refreshOverview();
            });
    }

    private startTimer(): void {
        this.stopTimer();
        this.now.set(new Date());
        this.timerInterval = setInterval(() => this.now.set(new Date()), 1000);
        this.destroyRef.onDestroy(() => this.stopTimer());
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
            .subscribe(overview => this.applyOverview(overview));
    }

    private applyOverview(overview: FastingOverview): void {
        this.currentSession.set(overview.currentSession);
        this.stats.set(overview.stats);
        this.history.set(overview.history.data);
        this.historyPage.set(overview.history.page);
        this.historyTotalPages.set(overview.history.totalPages);
        this.insightsData.set(overview.insights);
        this.syncCheckInFromSession(overview.currentSession);

        if (overview.currentSession && !overview.currentSession.endedAtUtc) {
            this.startTimer();
            return;
        }

        this.stopTimer();
    }

    private syncCheckInFromSession(session: FastingSession | null): void {
        this.hungerLevel.set(session?.hungerLevel ?? 3);
        this.energyLevel.set(session?.energyLevel ?? 3);
        this.moodLevel.set(session?.moodLevel ?? 3);
        this.selectedSymptoms.set(session?.symptoms ?? []);
        this.checkInNotes.set(session?.checkInNotes ?? '');
    }

    private getPromptStateKey(sessionId: string, promptId: string): string {
        return `${sessionId}:${promptId}`;
    }

    private updatePromptState(key: string, value: FastingPromptState): void {
        this.promptState.update(current => {
            const next = { ...current, [key]: { ...current[key], ...value } };
            this.writePromptState(next);
            return next;
        });
    }

    private clearPromptStateForSession(sessionId: string): void {
        this.promptState.update(current => {
            const next = Object.fromEntries(Object.entries(current).filter(([key]) => !key.startsWith(`${sessionId}:`)));
            this.writePromptState(next);
            return next;
        });
    }

    private readPromptState(): Record<string, FastingPromptState> {
        try {
            const stored = localStorage.getItem(FastingFacade.PromptStorageKey);
            if (!stored) {
                return {};
            }

            const parsed = JSON.parse(stored);
            return typeof parsed === 'object' && parsed !== null ? parsed : {};
        } catch {
            return {};
        }
    }

    private writePromptState(state: Record<string, FastingPromptState>): void {
        try {
            localStorage.setItem(FastingFacade.PromptStorageKey, JSON.stringify(state));
        } catch {
            // Ignore storage errors; prompts should still work in-memory.
        }
    }

    private formatDuration(ms: number): string {
        const totalSeconds = Math.floor(ms / 1000);
        const hours = Math.floor(totalSeconds / 3600);
        const minutes = Math.floor((totalSeconds % 3600) / 60);
        const seconds = totalSeconds % 60;
        return `${String(hours).padStart(2, '0')}:${String(minutes).padStart(2, '0')}:${String(seconds).padStart(2, '0')}`;
    }

    private getSessionDurationHours(session: FastingSession): number {
        const endedAt = session.endedAtUtc;
        if (!endedAt) {
            return 0;
        }

        const startedAtMs = new Date(session.startedAtUtc).getTime();
        const endedAtMs = new Date(endedAt).getTime();
        if (Number.isNaN(startedAtMs) || Number.isNaN(endedAtMs) || endedAtMs <= startedAtMs) {
            return 0;
        }

        return Math.round(((endedAtMs - startedAtMs) / 3_600_000) * 10) / 10;
    }

    private getReminderTelemetryDetails(): Record<string, unknown> {
        const user = this.userService.user();
        const firstReminderHours = user?.fastingCheckInReminderHours ?? 12;
        const followUpReminderHours = user?.fastingCheckInFollowUpReminderHours ?? 20;
        const reminderPresetId = resolveFastingReminderPresetId(firstReminderHours, followUpReminderHours);

        return {
            firstReminderHours,
            followUpReminderHours,
            reminderPresetId,
        };
    }

    private resetDraftState(): void {
        this.selectedMode.set('intermittent');
        this.selectedProtocol.set('F16_8');
        this.customHours.set(16);
        this.customIntermittentFastHours.set(16);
        this.cyclicEatDayProtocol.set('F16_8');
        this.cyclicFastDays.set(1);
        this.cyclicEatDays.set(1);
        this.cyclicUsesCustomPreset.set(false);
        this.cyclicEatDayFastHours.set(16);
        this.extendHours.set(24);
        this.reduceHours.set(4);
    }

    private getHistoryRange(): { from: string; to: string } {
        const now = new Date();
        const from = new Date(now.getFullYear(), now.getMonth() - 1, 1);
        const to = new Date(now.getFullYear(), now.getMonth() + 1, 0);

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
                cyclicEatDayEatingWindowHours: 24 - this.cyclicEatDayFastHours(),
            };
        }

        return {
            planType: selectedMode === 'intermittent' ? 'Intermittent' : 'Extended',
            protocol: this.selectedProtocol(),
            plannedDurationHours: this.plannedDurationHours(),
        };
    }
}
