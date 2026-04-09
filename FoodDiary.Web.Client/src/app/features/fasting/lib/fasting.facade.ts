import { DestroyRef, Injectable, computed, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { finalize, forkJoin, of } from 'rxjs';
import { FastingService } from '../api/fasting.service';
import { FASTING_PROTOCOLS, FastingMode, FastingPlanType, FastingProtocol, FastingSession, FastingStats } from '../models/fasting.data';

@Injectable()
export class FastingFacade {
    private readonly fastingService = inject(FastingService);
    private readonly destroyRef = inject(DestroyRef);
    private timerInterval: ReturnType<typeof setInterval> | null = null;

    public readonly isLoading = signal(false);
    public readonly isStarting = signal(false);
    public readonly isEnding = signal(false);
    public readonly isExtending = signal(false);
    public readonly isUpdatingCycle = signal(false);
    public readonly currentSession = signal<FastingSession | null>(null);
    public readonly stats = signal<FastingStats | null>(null);
    public readonly history = signal<FastingSession[]>([]);
    public readonly selectedMode = signal<FastingMode>('intermittent');
    public readonly selectedProtocol = signal<FastingProtocol>('F16_8');
    public readonly customHours = signal(16);
    public readonly customIntermittentFastHours = signal(16);
    public readonly cyclicFastDays = signal(1);
    public readonly cyclicEatDays = signal(1);
    public readonly cyclicEatDayFastHours = signal(16);
    public readonly extendHours = signal(24);
    public readonly now = signal(new Date());

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
        ])
            .pipe(
                finalize(() => this.isLoading.set(false)),
                takeUntilDestroyed(this.destroyRef),
            )
            .subscribe(([current, stats, history]) => {
                this.currentSession.set(current);
                this.stats.set(stats);
                this.history.set(history);

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
                if (session.endedAtUtc) {
                    this.stopTimer();
                    this.refreshStats();
                } else {
                    this.startTimer();
                    this.refreshHistory();
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
        this.cyclicFastDays.set(Math.max(1, Math.min(30, fastDays)));
        this.cyclicEatDays.set(Math.max(1, Math.min(30, eatDays)));
    }

    public setCyclicEatDayFastHours(hours: number): void {
        this.cyclicEatDayFastHours.set(Math.max(1, Math.min(23, hours)));
    }

    public setExtendHours(hours: number): void {
        this.extendHours.set(Math.max(1, Math.min(168, hours)));
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
            });
    }

    public skipCyclicFastDay(): void {
        this.isUpdatingCycle.set(true);
        this.fastingService
            .skipCyclicFastDay()
            .pipe(
                finalize(() => this.isUpdatingCycle.set(false)),
                takeUntilDestroyed(this.destroyRef),
            )
            .subscribe(session => {
                this.currentSession.set(session);
                this.startTimer();
                this.refreshHistory();
            });
    }

    public postponeCyclicFastDay(): void {
        this.isUpdatingCycle.set(true);
        this.fastingService
            .postponeCyclicFastDay()
            .pipe(
                finalize(() => this.isUpdatingCycle.set(false)),
                takeUntilDestroyed(this.destroyRef),
            )
            .subscribe(session => {
                this.currentSession.set(session);
                this.startTimer();
                this.refreshHistory();
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

    private formatDuration(ms: number): string {
        const totalSeconds = Math.floor(ms / 1000);
        const hours = Math.floor(totalSeconds / 3600);
        const minutes = Math.floor((totalSeconds % 3600) / 60);
        const seconds = totalSeconds % 60;
        return `${String(hours).padStart(2, '0')}:${String(minutes).padStart(2, '0')}:${String(seconds).padStart(2, '0')}`;
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
