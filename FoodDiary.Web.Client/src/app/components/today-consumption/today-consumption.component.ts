import { CommonModule } from '@angular/common';
import {
    ChangeDetectionStrategy,
    Component,
    DestroyRef,
    OnInit,
    computed,
    inject,
    signal,
    viewChild
} from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import { NavigationService } from '../../services/navigation.service';
import { NutrientData } from '../../types/charts.data';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button.component';
import { Consumption } from '../../types/consumption.data';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FdUiDatepicker, FdUiDatepickerInputEvent, FdUiDatepickerModule } from 'fd-ui-kit/material';
import { FdUiInputFieldModule } from 'fd-ui-kit/material';
import { FdUiFormFieldModule } from 'fd-ui-kit/material';
import { FdUiNativeDateModule } from 'fd-ui-kit/material';
import { FdUiIconModule } from 'fd-ui-kit/material';
import { PageHeaderComponent } from '../shared/page-header/page-header.component';
import { PageBodyComponent } from '../shared/page-body/page-body.component';
import { FdPageContainerDirective } from '../../directives/layout/page-container.directive';
import { DailyProgressCardComponent } from '../shared/daily-progress-card/daily-progress-card.component';
import { LocalizedDatePipe } from '../../pipes/localized-date.pipe';
import { MacroSummaryComponent } from '../shared/macro-summary/macro-summary.component';
import { MotivationCardComponent } from '../shared/motivation-card/motivation-card.component';
import { DashboardService } from '../../services/dashboard.service';
import { DashboardSnapshot } from '../../types/dashboard.data';
import { WeightSummaryCardComponent } from '../shared/weight-summary-card/weight-summary-card.component';
import { WaistSummaryCardComponent } from '../shared/waist-summary-card/waist-summary-card.component';
import { ActivityCardComponent } from '../shared/activity-card/activity-card.component';
import { QuickActionsSectionComponent } from '../shared/quick-actions/quick-actions-section/quick-actions-section.component';
import { MealCardComponent } from '../shared/meal-card/meal-card.component';

@Component({
    selector: 'fd-today-consumption',
    standalone: true,
    imports: [
        CommonModule,
        TranslatePipe,
        FdUiButtonComponent,
        FdUiDatepickerModule,
        FdUiInputFieldModule,
        FdUiFormFieldModule,
        FdUiNativeDateModule,
        FdUiIconModule,
        PageHeaderComponent,
        PageBodyComponent,
        FdPageContainerDirective,
        DailyProgressCardComponent,
        LocalizedDatePipe,
        MacroSummaryComponent,
        MotivationCardComponent,
        WeightSummaryCardComponent,
        WaistSummaryCardComponent,
        ActivityCardComponent,
        QuickActionsSectionComponent,
        MealCardComponent
    ],
    templateUrl: './today-consumption.component.html',
    styleUrl: './today-consumption.component.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class TodayConsumptionComponent implements OnInit {
    private readonly navigationService = inject(NavigationService);
    private readonly destroyRef = inject(DestroyRef);
    private readonly dashboardService = inject(DashboardService);

    private readonly headerDatePicker = viewChild<FdUiDatepicker<Date>>('headerDatePicker');

    public selectedDate = signal<Date>(this.normalizeDate(new Date()));
    public readonly isTodaySelected = computed(() => {
        const today = this.normalizeDate(new Date());
        return this.selectedDate().getTime() === today.getTime();
    });
    public snapshot = signal<DashboardSnapshot | null>(null);
    public isLoading = signal<boolean>(false);

    public readonly dailyGoal = computed(() => this.snapshot()?.dailyGoal ?? 0);
    public readonly todayCalories = computed(() => this.snapshot()?.statistics.totalCalories ?? 0);
    public readonly nutrientChartData = computed<NutrientData>(() => ({
        proteins: this.snapshot()?.statistics.averageProteins ?? 0,
        fats: this.snapshot()?.statistics.averageFats ?? 0,
        carbs: this.snapshot()?.statistics.averageCarbs ?? 0,
    }));
    public readonly todayFiber = computed(() => this.snapshot()?.statistics.averageFiber ?? null);
    public readonly meals = computed<Consumption[]>(() => this.snapshot()?.meals.items ?? []);
    public readonly latestWeight = computed(() => this.snapshot()?.weight.latest?.weight ?? null);
    public readonly previousWeight = computed(() => this.snapshot()?.weight.previous?.weight ?? null);
    public readonly desiredWeight = computed(() => this.snapshot()?.weight.desired ?? null);
    public readonly latestWaist = computed(() => this.snapshot()?.waist.latest?.circumference ?? null);
    public readonly previousWaist = computed(() => this.snapshot()?.waist.previous?.circumference ?? null);
    public readonly desiredWaist = computed(() => this.snapshot()?.waist.desired ?? null);

    public ngOnInit(): void {
        this.loadDashboardSnapshot();
    }

    public openDatePicker(): void {
        this.headerDatePicker()?.open();
    }

    public handleDateChange(event: FdUiDatepickerInputEvent<Date>): void {
        if (!event.value) {
            return;
        }

        const normalized = this.normalizeDate(event.value);

        if (normalized.getTime() !== this.selectedDate().getTime()) {
            this.selectedDate.set(normalized);
            this.fetchDashboardData();
        }
    }

    public async openWeightHistory(): Promise<void> {
        await this.navigationService.navigateToWeightHistory();
    }

    public async openWaistHistory(): Promise<void> {
        await this.navigationService.navigateToWaistHistory();
    }

    public openConsumption(consumption: Consumption): void {
        void this.navigationService.navigateToConsumptionEdit(consumption.id);
    }

    private fetchDashboardData(): void {
        this.loadDashboardSnapshot();
    }

    private loadDashboardSnapshot(): void {
        const targetDate = this.selectedDate();
        this.isLoading.set(true);

        this.dashboardService
            .getSnapshot(targetDate, 1, 10)
            .pipe(takeUntilDestroyed(this.destroyRef))
            .subscribe({
                next: snapshot => {
                    this.snapshot.set(snapshot);
                    this.isLoading.set(false);
                },
                error: () => {
                    this.snapshot.set(null);
                    this.isLoading.set(false);
                },
            });
    }

    private normalizeDate(date: Date): Date {
        const normalized = new Date(date);
        normalized.setHours(0, 0, 0, 0);
        return normalized;
    }

    public async addConsumption(): Promise<void> {
        await this.navigationService.navigateToConsumptionAdd();
    }

    public async manageConsumptions(): Promise<void> {
        await this.navigationService.navigateToConsumptionList();
    }
}
