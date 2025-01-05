import {
    ChangeDetectionStrategy,
    Component,
    computed,
    inject,
    OnInit,
    signal
} from '@angular/core';
import { StatisticsService } from '../../services/statistics.service';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { TuiButton } from '@taiga-ui/core';
import { DecimalPipe } from '@angular/common';
import { NavigationService } from '../../services/navigation.service';
import { ChartData, ChartOptions } from 'chart.js';
import { BaseChartDirective } from 'ng2-charts';
import { NutrientChartData } from '../../types/charts.data';

@Component({
    selector: 'app-today-consumption',
    imports: [TranslatePipe, DecimalPipe, TuiButton, BaseChartDirective],
    templateUrl: './today-consumption.component.html',
    styleUrl: './today-consumption.component.less',
    changeDetection: ChangeDetectionStrategy.OnPush
})
export class TodayConsumptionComponent implements OnInit {
    private readonly statisticsService = inject(StatisticsService);
    private readonly translateService = inject(TranslateService);
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

    public pieChartData = computed<ChartData<'pie', number[], string>>(() => ({
        labels: [
            this.translateService.instant('STATISTICS.NUTRIENTS.PROTEINS'),
            this.translateService.instant('STATISTICS.NUTRIENTS.FATS'),
            this.translateService.instant('STATISTICS.NUTRIENTS.CARBS'),
        ],
        datasets: [
            {
                data: [
                    this.nutrientChartData().proteins,
                    this.nutrientChartData().fats,
                    this.nutrientChartData().carbs,
                ],
                backgroundColor: ['#36A2EB', '#FFCE56', '#4BC0C0'],
            },
        ],
    }));

    public pieChartOptions: ChartOptions<'pie'> = {
        responsive: true,
        plugins: {
            tooltip: {
                callbacks: {
                    label: (context) => {
                        const label = context.label || '';
                        const value = Number(context.raw) || 0;
                        const formattedValue = parseFloat(value.toFixed(2));

                        return `${label}: ${formattedValue} ${this.translateService.instant('STATISTICS.GRAMS')}`;
                    },
                },
            },
        },
    };

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
