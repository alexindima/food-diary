import { DestroyRef, Injectable, computed, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { finalize } from 'rxjs';
import { WeeklyCheckInService } from '../api/weekly-check-in.service';
import { WeeklyCheckInData } from '../models/weekly-check-in.data';

@Injectable()
export class WeeklyCheckInFacade {
    private readonly service = inject(WeeklyCheckInService);
    private readonly destroyRef = inject(DestroyRef);

    public readonly isLoading = signal(false);
    public readonly data = signal<WeeklyCheckInData | null>(null);

    public readonly thisWeek = computed(() => this.data()?.thisWeek);
    public readonly lastWeek = computed(() => this.data()?.lastWeek);
    public readonly trends = computed(() => this.data()?.trends);
    public readonly suggestions = computed(() => this.data()?.suggestions ?? []);

    public initialize(): void {
        this.isLoading.set(true);
        this.service
            .getData()
            .pipe(
                finalize(() => this.isLoading.set(false)),
                takeUntilDestroyed(this.destroyRef),
            )
            .subscribe(data => this.data.set(data));
    }

    public getTrendIcon(value: number): string {
        if (value > 0) {
            return 'trending_up';
        }
        if (value < 0) {
            return 'trending_down';
        }
        return 'trending_flat';
    }

    public getTrendColor(value: number, invertPositive = false): string {
        if (value === 0) {
            return 'var(--fd-color-slate-500)';
        }
        const isPositive = invertPositive ? value < 0 : value > 0;
        return isPositive ? 'var(--fd-color-green-500)' : '#ef4444';
    }
}
