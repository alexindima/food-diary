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
import { FdUiEntityCardComponent } from 'fd-ui-kit/entity-card/fd-ui-entity-card.component';
import { Consumption } from '../../types/consumption.data';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FdUiDatepicker, FdUiDatepickerInputEvent, FdUiDatepickerModule } from 'fd-ui-kit/material';
import { FdUiInputFieldModule } from 'fd-ui-kit/material';
import { FdUiFormFieldModule } from 'fd-ui-kit/material';
import { FdUiNativeDateModule } from 'fd-ui-kit/material';
import { FdUiIconModule } from 'fd-ui-kit/material';
import { WeightEntry } from '../../types/weight-entry.data';
import { WaistEntry } from '../../types/waist-entry.data';
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

@Component({
    selector: 'fd-today-consumption',
    standalone: true,
    imports: [
        CommonModule,
        TranslatePipe,
        FdUiButtonComponent,
        FdUiEntityCardComponent,
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
        QuickActionsSectionComponent
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
    public todayCalories = signal<number>(0);
    public todayFiber = signal<number | null>(null);
    public nutrientChartData = signal<NutrientData>({
        proteins: 0,
        fats: 0,
        carbs: 0,
    });
    public meals = signal<Consumption[]>([]);
    public isLoading = signal<boolean>(false);
    public dailyGoal = signal<number>(2000);
    public latestWeightEntry = signal<WeightEntry | null>(null);
    public previousWeightEntry = signal<WeightEntry | null>(null);
    public desiredWeight = signal<number | null>(null);
    public latestWaistEntry = signal<WaistEntry | null>(null);
    public previousWaistEntry = signal<WaistEntry | null>(null);
    public desiredWaist = signal<number | null>(null);

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
                    if (!snapshot) {
                        this.resetSnapshotState();
                        return;
                    }
                    this.applySnapshot(snapshot);
                },
                error: () => this.resetSnapshotState(),
            });
    }

    private applySnapshot(snapshot: DashboardSnapshot): void {
        this.dailyGoal.set(snapshot.dailyGoal || 0);

        this.todayCalories.set(snapshot.statistics.totalCalories || 0);
        this.nutrientChartData.set({
            proteins: snapshot.statistics.averageProteins || 0,
            fats: snapshot.statistics.averageFats || 0,
            carbs: snapshot.statistics.averageCarbs || 0,
        });
        this.todayFiber.set(snapshot.statistics.averageFiber ?? null);

        const latestWeight = snapshot.weight.latest
            ? { id: '', userId: '', weight: snapshot.weight.latest.weight, date: new Date(snapshot.weight.latest.date) }
            : null;
        const previousWeight = snapshot.weight.previous
            ? { id: '', userId: '', weight: snapshot.weight.previous.weight, date: new Date(snapshot.weight.previous.date) }
            : null;
        this.latestWeightEntry.set(latestWeight as WeightEntry | null);
        this.previousWeightEntry.set(previousWeight as WeightEntry | null);
        this.desiredWeight.set(snapshot.weight.desired ?? null);

        const latestWaist = snapshot.waist.latest
            ? { id: '', userId: '', circumference: snapshot.waist.latest.circumference, date: new Date(snapshot.waist.latest.date) }
            : null;
        const previousWaist = snapshot.waist.previous
            ? { id: '', userId: '', circumference: snapshot.waist.previous.circumference, date: new Date(snapshot.waist.previous.date) }
            : null;
        this.latestWaistEntry.set(latestWaist as WaistEntry | null);
        this.previousWaistEntry.set(previousWaist as WaistEntry | null);
        this.desiredWaist.set(snapshot.waist.desired ?? null);

        this.meals.set(snapshot.meals.items ?? []);

        this.isLoading.set(false);
    }

    private resetSnapshotState(): void {
        this.dailyGoal.set(0);
        this.todayCalories.set(0);
        this.nutrientChartData.set({ proteins: 0, fats: 0, carbs: 0 });
        this.todayFiber.set(null);
        this.meals.set([]);
        this.latestWeightEntry.set(null);
        this.previousWeightEntry.set(null);
        this.desiredWeight.set(null);
        this.latestWaistEntry.set(null);
        this.previousWaistEntry.set(null);
        this.desiredWaist.set(null);

        this.isLoading.set(false);
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
