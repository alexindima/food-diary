import { DestroyRef, Injectable, computed, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { finalize, forkJoin } from 'rxjs';
import { FastingService } from '../api/fasting.service';
import { FASTING_PROTOCOLS, FastingProtocol, FastingSession, FastingStats } from '../models/fasting.data';

@Injectable()
export class FastingFacade {
    private readonly fastingService = inject(FastingService);
    private readonly destroyRef = inject(DestroyRef);
    private timerInterval: ReturnType<typeof setInterval> | null = null;

    public readonly isLoading = signal(false);
    public readonly isStarting = signal(false);
    public readonly isEnding = signal(false);
    public readonly currentSession = signal<FastingSession | null>(null);
    public readonly stats = signal<FastingStats | null>(null);
    public readonly history = signal<FastingSession[]>([]);
    public readonly selectedProtocol = signal<FastingProtocol>('F16_8');
    public readonly customHours = signal(16);
    public readonly now = signal(new Date());

    public readonly isActive = computed(() => {
        const session = this.currentSession();
        return session !== null && session.endedAtUtc === null;
    });

    public readonly plannedDurationHours = computed(() => {
        const protocol = this.selectedProtocol();
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

    public initialize(): void {
        this.isLoading.set(true);

        const now = new Date();
        const from = new Date(now.getFullYear(), now.getMonth() - 1, 1);
        const to = new Date(now.getFullYear(), now.getMonth() + 1, 0);

        forkJoin([
            this.fastingService.getCurrent(),
            this.fastingService.getStats(),
            this.fastingService.getHistory({
                from: from.toISOString(),
                to: to.toISOString(),
            }),
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
        this.fastingService
            .start({
                protocol: this.selectedProtocol(),
                plannedDurationHours: this.plannedDurationHours(),
            })
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
                this.stopTimer();
                this.refreshStats();
            });
    }

    public selectProtocol(protocol: FastingProtocol): void {
        this.selectedProtocol.set(protocol);
    }

    public setCustomHours(hours: number): void {
        this.customHours.set(Math.max(1, Math.min(72, hours)));
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
}
