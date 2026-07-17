import { Service, signal } from '@angular/core';

@Service()
export class NutritionDataInvalidationService {
    private readonly mealsVersionState = signal(0);
    private readonly dashboardVersionState = signal(0);
    private readonly statisticsVersionState = signal(0);

    public readonly mealsVersion = this.mealsVersionState.asReadonly();
    public readonly dashboardVersion = this.dashboardVersionState.asReadonly();
    public readonly statisticsVersion = this.statisticsVersionState.asReadonly();

    public reportMealMutation(): void {
        this.mealsVersionState.update(incrementVersion);
        this.dashboardVersionState.update(incrementVersion);
        this.statisticsVersionState.update(incrementVersion);
    }

    public reportGoalMutation(): void {
        this.dashboardVersionState.update(incrementVersion);
        this.statisticsVersionState.update(incrementVersion);
    }

    public reportBodyMetricMutation(): void {
        this.dashboardVersionState.update(incrementVersion);
        this.statisticsVersionState.update(incrementVersion);
    }
}

function incrementVersion(version: number): number {
    return version + 1;
}
