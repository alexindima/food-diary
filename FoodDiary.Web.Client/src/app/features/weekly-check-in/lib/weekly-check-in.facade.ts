import { computed, effect, inject, Injectable, resource, signal } from '@angular/core';
import { firstValueFrom } from 'rxjs';

import { WeeklyCheckInService } from '../api/weekly-check-in.service';
import type { WeeklyCheckInData } from '../models/weekly-check-in.data';
import { buildWeeklyCheckInSuggestionRows, buildWeeklyCheckInTrendCards } from './weekly-check-in.mapper';
import { buildWeeklyReview } from './weekly-review.mapper';

const DAYS_PER_WEEK = 7;
const MONDAY_OFFSET = 6;

@Injectable()
export class WeeklyCheckInFacade {
    private readonly service = inject(WeeklyCheckInService);
    private readonly lastLoadedData = signal<WeeklyCheckInData | null>(null);
    public readonly selectedWeek = signal(startOfLocalWeek(new Date()));
    private readonly dataResource = resource({
        params: () => formatLocalDate(this.selectedWeek()),
        loader: async ({ params }): Promise<WeeklyCheckInData> => firstValueFrom(this.service.getData(params)),
    });

    public readonly data = computed(() => (this.dataResource.hasValue() ? this.dataResource.value() : this.lastLoadedData()));
    public readonly isLoading = computed(() => this.dataResource.isLoading() && this.data() === null);
    public readonly isRefreshing = computed(() => this.dataResource.isLoading() && this.data() !== null);

    public readonly thisWeek = computed(() => this.data()?.thisWeek);
    public readonly trends = computed(() => this.data()?.trends);
    public readonly suggestions = computed(() => this.data()?.suggestions ?? []);
    public readonly suggestionRows = computed(() => buildWeeklyCheckInSuggestionRows(this.suggestions()));
    public readonly trendCards = computed(() => buildWeeklyCheckInTrendCards(this.trends()));
    public readonly review = computed(() => buildWeeklyReview(this.thisWeek(), this.trends(), this.suggestions()));

    public constructor() {
        effect(() => {
            if (this.dataResource.hasValue()) {
                this.lastLoadedData.set(this.dataResource.value());
            }
        });
    }

    public initialize(): void {
        this.dataResource.reload();
    }
}

function formatLocalDate(date: Date): string {
    const year = date.getFullYear();
    const month = String(date.getMonth() + 1).padStart(2, '0');
    const day = String(date.getDate()).padStart(2, '0');
    return `${year}-${month}-${day}`;
}

function startOfLocalWeek(date: Date): Date {
    const result = new Date(date.getFullYear(), date.getMonth(), date.getDate());
    const daysSinceMonday = (result.getDay() + MONDAY_OFFSET) % DAYS_PER_WEEK;
    result.setDate(result.getDate() - daysSinceMonday);
    return result;
}
