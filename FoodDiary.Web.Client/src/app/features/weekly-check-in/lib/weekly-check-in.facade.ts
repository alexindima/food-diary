import { computed, effect, inject, Injectable, resource, signal } from '@angular/core';
import { firstValueFrom } from 'rxjs';

import { WeeklyCheckInService } from '../api/weekly-check-in.service';
import { WeeklyGoalService } from '../api/weekly-goal.service';
import type { WeeklyCheckInData } from '../models/weekly-check-in.data';
import type { UpsertWeeklyGoalPayload, WeeklyGoal } from '../models/weekly-goal.data';
import { buildWeeklyCheckInSuggestionRows, buildWeeklyCheckInTrendCards } from './weekly-check-in.mapper';
import { buildWeeklyReview } from './weekly-review.mapper';

const DAYS_PER_WEEK = 7;
const MONDAY_OFFSET = 6;

@Injectable()
export class WeeklyCheckInFacade {
    private readonly service = inject(WeeklyCheckInService);
    private readonly goalService = inject(WeeklyGoalService);
    private readonly lastLoadedData = signal<WeeklyCheckInData | null>(null);
    public readonly selectedWeek = signal(startOfLocalWeek(new Date()));
    private readonly currentWeekStart = startOfLocalWeek(new Date());
    private readonly dataResource = resource({
        params: () => formatLocalDate(this.selectedWeek()),
        loader: async ({ params }): Promise<WeeklyCheckInData> => firstValueFrom(this.service.getData(params)),
    });
    public readonly goalWeekStart = computed(() => addLocalDays(this.selectedWeek(), DAYS_PER_WEEK));
    public readonly isSelectedWeekPast = computed(() => this.selectedWeek().getTime() < this.currentWeekStart.getTime());
    public readonly isGoalPeriodClosed = computed(() => this.goalWeekStart().getTime() < this.currentWeekStart.getTime());
    public readonly goalWeekStartIso = computed(() => formatLocalDate(this.goalWeekStart()));
    public readonly selectedWeekStartIso = computed(() => formatLocalDate(this.selectedWeek()));
    private readonly goalResource = resource({
        params: () => formatLocalDate(this.goalWeekStart()),
        loader: async ({ params }): Promise<WeeklyGoal | null> => firstValueFrom(this.goalService.getGoal(params)),
    });
    private readonly selectedWeekGoalResource = resource({
        params: () => formatLocalDate(this.selectedWeek()),
        loader: async ({ params }): Promise<WeeklyGoal | null> => firstValueFrom(this.goalService.getGoal(params)),
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
    public readonly weeklyGoal = computed(() => (this.goalResource.hasValue() ? this.goalResource.value() : null));
    public readonly selectedWeekGoal = computed(() =>
        this.selectedWeekGoalResource.hasValue() ? this.selectedWeekGoalResource.value() : null,
    );
    public readonly isGoalLoading = computed(() => this.goalResource.isLoading());
    public readonly isSelectedWeekGoalLoading = computed(() => this.selectedWeekGoalResource.isLoading());

    public constructor() {
        effect(() => {
            if (this.dataResource.hasValue()) {
                this.lastLoadedData.set(this.dataResource.value());
            }
        });
    }

    public initialize(): void {
        this.dataResource.reload();
        this.goalResource.reload();
        this.selectedWeekGoalResource.reload();
    }

    public async saveGoalAsync(payload: UpsertWeeklyGoalPayload): Promise<WeeklyGoal | null> {
        const goal = await firstValueFrom(this.goalService.upsertGoal(payload));
        this.goalResource.reload();
        this.selectedWeekGoalResource.reload();
        return goal;
    }

    public reloadGoal(): void {
        this.goalResource.reload();
        this.selectedWeekGoalResource.reload();
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

function addLocalDays(date: Date, days: number): Date {
    const result = new Date(date.getFullYear(), date.getMonth(), date.getDate());
    result.setDate(result.getDate() + days);
    return result;
}
