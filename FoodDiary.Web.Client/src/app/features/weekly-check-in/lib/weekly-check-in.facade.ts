import { computed, inject, Injectable, resource } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { WeeklyCheckInService } from '../api/weekly-check-in.service';
import { WeeklyCheckInData } from '../models/weekly-check-in.data';

@Injectable()
export class WeeklyCheckInFacade {
    private readonly service = inject(WeeklyCheckInService);
    private readonly dataResource = resource({
        loader: async (): Promise<WeeklyCheckInData> => firstValueFrom(this.service.getData()),
    });

    public readonly isLoading = computed(() => this.dataResource.isLoading());
    public readonly data = computed(() => (this.dataResource.hasValue() ? this.dataResource.value() : null));

    public readonly thisWeek = computed(() => this.data()?.thisWeek);
    public readonly lastWeek = computed(() => this.data()?.lastWeek);
    public readonly trends = computed(() => this.data()?.trends);
    public readonly suggestions = computed(() => this.data()?.suggestions ?? []);

    public initialize(): void {
        this.dataResource.reload();
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
