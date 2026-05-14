import { computed, inject, Injectable, resource } from '@angular/core';
import { firstValueFrom } from 'rxjs';

import { WeeklyCheckInService } from '../api/weekly-check-in.service';
import type { WeeklyCheckInData } from '../models/weekly-check-in.data';
import { buildWeeklyCheckInSuggestionRows, buildWeeklyCheckInTrendCards } from './weekly-check-in.mapper';

@Injectable({ providedIn: 'root' })
export class WeeklyCheckInFacade {
    private readonly service = inject(WeeklyCheckInService);
    private readonly dataResource = resource({
        loader: async (): Promise<WeeklyCheckInData> => firstValueFrom(this.service.getData()),
    });

    public readonly isLoading = computed(() => this.dataResource.isLoading());
    public readonly data = computed(() => (this.dataResource.hasValue() ? this.dataResource.value() : null));

    public readonly thisWeek = computed(() => this.data()?.thisWeek);
    public readonly trends = computed(() => this.data()?.trends);
    public readonly suggestions = computed(() => this.data()?.suggestions ?? []);
    public readonly suggestionRows = computed(() => buildWeeklyCheckInSuggestionRows(this.suggestions()));
    public readonly trendCards = computed(() => buildWeeklyCheckInTrendCards(this.trends()));

    public initialize(): void {
        this.dataResource.reload();
    }
}
