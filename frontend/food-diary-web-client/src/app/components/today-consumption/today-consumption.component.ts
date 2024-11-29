import { Component, inject, OnInit, signal } from '@angular/core';
import { StatisticsService } from '../../services/statistics.service';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { TuiButton, TuiHintOptionsDirective } from '@taiga-ui/core';
import { DecimalPipe } from '@angular/common';
import { TuiPieChart } from '@taiga-ui/addon-charts';
import { NavigationService } from '../../services/navigation.service';

@Component({
    selector: 'app-today-consumption',
    standalone: true,
    imports: [TranslatePipe, DecimalPipe, TuiButton, TuiPieChart, TuiHintOptionsDirective],
    templateUrl: './today-consumption.component.html',
    styleUrl: './today-consumption.component.less',
})
export class TodayConsumptionComponent implements OnInit {
    private readonly statisticsService = inject(StatisticsService);
    private readonly translateService = inject(TranslateService);
    private readonly navigationService = inject(NavigationService);

    public todayCalories: number | null = null;
    public nutrientChartData = { values: [0, 0, 0], labels: [] as string[] };
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
                        this.todayCalories = stats?.totalCalories ?? 0;

                        this.nutrientChartData = {
                            values: [stats?.averageProteins ?? 0, stats?.averageFats ?? 0, stats?.averageCarbs ?? 0],
                            labels: [
                                this.translateService.instant('STATISTICS.NUTRIENTS.PROTEINS'),
                                this.translateService.instant('STATISTICS.NUTRIENTS.FATS'),
                                this.translateService.instant('STATISTICS.NUTRIENTS.CARBS'),
                            ],
                        };
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
