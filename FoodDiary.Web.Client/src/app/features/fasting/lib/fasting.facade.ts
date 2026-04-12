import { DestroyRef, Injectable, computed, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { finalize, forkJoin, of } from 'rxjs';
import { FastingService } from '../api/fasting.service';
import { FrontendObservabilityService } from '../../../services/frontend-observability.service';
import { UserService } from '../../../shared/api/user.service';
import { resolveFastingReminderPresetId } from '../../../shared/lib/fasting-reminder-presets';
import {
    FASTING_PROTOCOLS,
    FastingInsights,
    FastingMessage,
    FastingMode,
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

    private readonly fastingService = inject(FastingService);
    private readonly frontendObservability = inject(FrontendObservabilityService);
    private readonly userService = inject(UserService);
    private readonly destroyRef = inject(DestroyRef);
    private timerInterval: ReturnType<typeof setInterval> | null = null;

    public readonly isLoading = signal(false);
    public readonly isStarting = signal(false);
    public readonly isEnding = signal(false);
    public readonly isExtending = signal(false);
    public readonly isUpdatingCycle = signal(false);
    public readonly isSavingCheckIn = signal(false);
    public readonly currentSession = signal<FastingSession | null>(null);
    public readonly stats = signal<FastingStats | null>(null);
    public readonly history = signal<FastingSession[]>([]);
    public readonly insightsData = signal<FastingInsights>({ insights: [], currentPrompt: null });
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

    public initialize(options?: { includeStats?: boolean; includeHistory?: boolean }): void {
        this.isLoading.set(true);
        const includeStats = options?.includeStats ?? true;
        const includeHistory = options?.includeHistory ?? true;

        const now = new Date();
        const from = new Date(now.getFullYear(), now.getMonth() - 1, 1);
        const to = new Date(now.getFullYear(), now.getMonth() + 1, 0);

        forkJoin([
            this.fastingService.getCurrent(),
            includeStats ? this.fastingService.getStats() : of(null),
            includeHistory
                ? this.fastingService.getHistory({
                      from: from.toISOString(),
                      to: to.toISOString(),
                  })
                : of([]),
            this.fastingService.getInsights(),
        ])
            .pipe(
                finalize(() => this.isLoading.set(false)),
                takeUntilDestroyed(this.destroyRef),
            )
            .subscribe(([current, stats, history, insights]) => {
                this.currentSession.set(current);
                this.stats.set(stats);
                this.history.set(history);
                this.insightsData.set(insights);
                this.syncCheckInFromSession(current);

                if (current && !current.endedAtUtc) {
                    this.startTimer();
                }
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
                this.refreshInsights();
                this.frontendObservability.recordFastingLifecycleEvent('session.started', {
                    sessionId: session.id,
                    protocol: session.protocol,
                    planType: session.planType,
                    plannedDurationHours: session.plannedDurationHours,
                    occurrenceKind: session.occurrenceKind,
                    ...this.getReminderTelemetryDetails(),
                });
                this.startTimer();
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
                this.currentSession.set(session);
                this.syncCheckInFromSession(session);
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
                    this.refreshStats();
                } else {
                    this.startTimer();
                    this.refreshHistory();
                    this.refreshInsights();
                }
            });
    }

    public selectProtocol(protocol: FastingProtocol): void {
        this.selectedProtocol.set(protocol);
    }

    public selectMode(mode: FastingMode): void {
        this.selectedMode.set(mode);
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
                this.refreshInsights();
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
                this.refreshHistory();
                this.refreshInsights();
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
                this.refreshHistory();
                this.refreshInsights();
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
                this.refreshHistory();
                this.refreshInsights();
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

    private refreshStats(): void {
        this.fastingService
            .getStats()
            .pipe(takeUntilDestroyed(this.destroyRef))
            .subscribe(stats => this.stats.set(stats));

        this.refreshHistory();
        this.refreshInsights();
    }

    private refreshHistory(): void {
        const now = new Date();
        const from = new Date(now.getFullYear(), now.getMonth() - 1, 1);
        const to = new Date(now.getFullYear(), now.getMonth() + 1, 0);

        this.fastingService
            .getHistory({ from: from.toISOString(), to: to.toISOString() })
            .pipe(takeUntilDestroyed(this.destroyRef))
            .subscribe(history => this.history.set(history));
    }

    private refreshInsights(): void {
        this.fastingService
            .getInsights()
            .pipe(takeUntilDestroyed(this.destroyRef))
            .subscribe(insights => this.insightsData.set(insights));
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
