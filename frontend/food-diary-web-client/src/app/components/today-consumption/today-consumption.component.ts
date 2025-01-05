import { ChangeDetectionStrategy, Component, inject, OnInit, signal } from '@angular/core';
import { StatisticsService } from '../../services/statistics.service';
import { TranslatePipe } from '@ngx-translate/core';
import { TuiButton } from '@taiga-ui/core';
import { NavigationService } from '../../services/navigation.service';
import { NutrientChartData } from '../../types/charts.data';
import { NutrientsSummaryComponent } from '../shared/nutrients-summary/nutrients-summary.component';

@Component({
    selector: 'app-today-consumption',
    imports: [
        TranslatePipe,
        TuiButton,
        NutrientsSummaryComponent
    ],
    templateUrl: './today-consumption.component.html',
    styleUrl: './today-consumption.component.less',
    changeDetection: ChangeDetectionStrategy.OnPush
})
export class TodayConsumptionComponent implements OnInit {
    private readonly statisticsService = inject(StatisticsService);
    private readonly navigationService = inject(NavigationService);

    public todayCalories = signal<number>(0);
    public nutrientChartData = signal<NutrientChartData>({
        proteins: 0,
        fats: 0,
        carbs: 0,
    });
    public isLoading = signal<boolean>(false);

    public ngOnInit(): void {
        this.fetchTodayData();
    }

    private fetchTodayData(): void {
        this.isLoading.set(true);
        const today = new Date();
        this.statisticsService
            .getAggregatedStatistics({
                dateFrom: today,
                dateTo: today,
            })
            .subscribe({
                next: response => {
                    if (response.status === 'success') {
                        const stats = response.data![0];
                        this.todayCalories.set(stats.totalCalories);

                        this.nutrientChartData.set({
                            proteins: stats?.averageProteins,
                            fats: stats?.averageFats,
                            carbs: stats?.averageCarbs,
                        });
                    }
                    this.isLoading.set(false);
                },
                error: () => {
                    this.isLoading.set(false);
                },
            });
    }

    public async addConsumption(): Promise<void> {
        await this.navigationService.navigateToConsumptionAdd();
    }

    public async addFood(): Promise<void> {
        await this.navigationService.navigateToFoodAdd();
    }

    public async manageConsumptions(): Promise<void> {
        await this.navigationService.navigateToConsumptionList();
    }

    public async manageFoods(): Promise<void> {
        await this.navigationService.navigateToFoodList();
    }

    public async goToStatistics(): Promise<void> {
        await this.navigationService.navigateToStatistics();
    }
}
