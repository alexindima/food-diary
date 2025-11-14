import { ChangeDetectionStrategy, Component, inject, OnInit, signal } from '@angular/core';
import { StatisticsService } from '../../services/statistics.service';
import { TranslatePipe } from '@ngx-translate/core';
import { TuiButton } from '@taiga-ui/core';
import { NavigationService } from '../../services/navigation.service';
import { NutrientChartData } from '../../types/charts.data';
import { NutrientsSummaryComponent } from '../shared/nutrients-summary/nutrients-summary.component';
import {
    DynamicProgressBarComponent
} from '../shared/dynamic-progress-bar/dynamic-progress-bar.component';
import { CustomGroupComponent } from '../shared/custom-group/custom-group.component';
import { UserService } from '../../services/user.service';
import { Observable } from 'rxjs';
import { AsyncPipe } from '@angular/common';

@Component({
    selector: 'fd-today-consumption',
    imports: [
        TranslatePipe,
        TuiButton,
        NutrientsSummaryComponent,
        DynamicProgressBarComponent,
        CustomGroupComponent,
        AsyncPipe
    ],
    templateUrl: './today-consumption.component.html',
    styleUrl: './today-consumption.component.less',
    changeDetection: ChangeDetectionStrategy.OnPush
})
export class TodayConsumptionComponent implements OnInit {
    private readonly statisticsService = inject(StatisticsService);
    private readonly navigationService = inject(NavigationService);
    private readonly userService = inject(UserService);

    public test = signal(500);
    public todayCalories = signal<number>(0);
    public todayFiber = signal<number | null>(null);
    public nutrientChartData = signal<NutrientChartData>({
        proteins: 0,
        fats: 0,
        carbs: 0,
    });
    public isLoading = signal<boolean>(false);

    public userCalories: Observable<number | null> = this.userService.getUserCalories();

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
                quantizationDays: 1,
            })
            .subscribe({
                next: data => {
                    const stats = data?.[0];
                    this.todayCalories.set(stats?.totalCalories ?? 0);

                    this.nutrientChartData.set({
                        proteins: stats?.averageProteins ?? 0,
                        fats: stats?.averageFats ?? 0,
                        carbs: stats?.averageCarbs ?? 0,
                    });
                    this.todayFiber.set(stats?.averageFiber ?? null);
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

    public async addProduct(): Promise<void> {
        await this.navigationService.navigateToProductAdd();
    }

    public async manageConsumptions(): Promise<void> {
        await this.navigationService.navigateToConsumptionList();
    }

    public async manageProducts(): Promise<void> {
        await this.navigationService.navigateToProductList();
    }

    public async goToStatistics(): Promise<void> {
        await this.navigationService.navigateToStatistics();
    }
}
