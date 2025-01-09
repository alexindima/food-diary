import {
    ChangeDetectionStrategy,
    Component,
    computed,
    HostListener,
    inject,
    OnInit,
    signal
} from '@angular/core';
import { MappedStatistics, StatisticsMapper } from '../../types/statistics.data';
import { StatisticsService } from '../../services/statistics.service';
import { TUI_MONTHS, TuiGroup, TuiLoader } from '@taiga-ui/core';
import { TuiDay, TuiDayLike, TuiDayRange } from '@taiga-ui/cdk';
import { finalize, map, Observable } from 'rxjs';
import { TuiInputDateRangeModule } from '@taiga-ui/legacy';
import { FormControl, FormGroup, FormsModule, ReactiveFormsModule } from '@angular/forms';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { TuiBlock } from '@taiga-ui/kit';
import { FormGroupControls } from '../../types/common.data';
import { BaseChartDirective } from 'ng2-charts';
import { ChartData, ChartOptions, ChartTypeRegistry, TooltipItem } from 'chart.js';
import { toSignal } from '@angular/core/rxjs-interop';
import { CHART_COLORS } from '../../constants/chart-colors';

@Component({
    selector: 'fd-statistics',
    imports: [
        TuiInputDateRangeModule,
        FormsModule,
        TuiLoader,
        TranslatePipe,
        TuiGroup,
        ReactiveFormsModule,
        TuiBlock,
        BaseChartDirective,
    ],
    templateUrl: './statistics.component.html',
    styleUrls: ['./statistics.component.less'],
    changeDetection: ChangeDetectionStrategy.OnPush
})
export class StatisticsComponent implements OnInit {
    protected readonly maxLength: TuiDayLike = {year: 1};
    private readonly defaultRange: RangeMode = 'Month';

    private readonly statisticsService = inject(StatisticsService);
    private readonly translateService = inject(TranslateService);
    private readonly months$: Observable<string[]> = inject(TUI_MONTHS).pipe(
        map(months => Object.values(months))
    );

    public isLoading = signal<boolean>(false);
    public isMobile = signal<boolean>(false);
    public months = toSignal(this.months$, {initialValue: []});
    public data = signal(new TuiDayRange(TuiDay.currentLocal().append({month: -1}), TuiDay.currentLocal()));
    public chartStatisticsData = signal<MappedStatistics | null>(null);
    public computedRange = computed(() => this.computeRange(this.data()));

    protected readonly rangeForm: FormGroup<StatisticsFormData> = new FormGroup({
        range: new FormControl<RangeMode>(this.defaultRange, {nonNullable: true}),
        inputRange: new FormControl<TuiDayRange | null>(
            new TuiDayRange(TuiDay.currentLocal().append({month: -1}), TuiDay.currentLocal()),
            {nonNullable: false},
        ),
    });

    public ngOnInit(): void {
        this.checkScreenWidth();
        this.updateDateRange(this.defaultRange);

        this.rangeForm.controls.range.valueChanges.subscribe(range => {
            if (range) {
                this.updateDateRange(range);
            }
        });

        this.rangeForm.controls.inputRange.valueChanges.subscribe(inputRange => {
            if (this.rangeForm.controls.range.value === 'Custom' && inputRange) {
                this.data.set(inputRange);
                this.updateCharts();
            }
        });
    }

    @HostListener('window:resize', [])
    public onResize(): void {
        this.checkScreenWidth();
    }

    private checkScreenWidth(): void {
        this.isMobile.set(window.innerWidth <= 600);
    }

    private updateCharts(): void {
        this.isLoading.set(true);

        this.statisticsService
            .getAggregatedStatistics({
                dateFrom: this.data().from.toLocalNativeDate(),
                dateTo: this.data().to.toLocalNativeDate(),
            })
            .pipe(finalize(() => this.isLoading.set(false)))
            .subscribe({
                next: response => {
                    if (response.status === 'success') {
                        const mappedData = StatisticsMapper.mapStatistics(response.data!);
                        this.chartStatisticsData.set(mappedData);
                    }
                }
            });
    }

    private updateDateRange(range: RangeMode): void {
        const now = TuiDay.currentLocal();
        switch (range) {
            case 'Week':
                this.data.set(new TuiDayRange(now.append({day: -7}), now));
                break;
            case 'Month':
                this.data.set(new TuiDayRange(now.append({month: -1}), now));
                break;
            case 'Year':
                this.data.set(new TuiDayRange(now.append({year: -1}), now));
                break;
            case 'Custom':
                const inputRange = this.rangeForm.controls.inputRange.value;
                if (inputRange) {
                    this.data.set(new TuiDayRange(inputRange.from, inputRange.to));
                }
                break;
            default:
                break;
        }

        this.updateCharts();
    }

    public caloriesLineChartOptions: ChartOptions<'line'> = {
        responsive: true,
        plugins: {
            tooltip: {
                callbacks: {
                    label: (context) => this.getFormattedTooltip(context, 'STATISTICS.KKAL'),
                }
            }
        },
        scales: {
            y: {
                min: 0
            }
        }
    };

    public baseNutrientsChartOptions = {
        responsive: true,
        plugins: {
            tooltip: {
                callbacks: {
                    label: (context: TooltipItem<any>): string => this.getFormattedTooltip(context, 'STATISTICS.GRAMS'),
                }
            }
        }
    };

    public nutrientsLineChartOptions: ChartOptions<'line'> = {
        ...this.baseNutrientsChartOptions,
        scales: {
            y: {
                min: 0
            }
        }
    };

    public pieChartOptions: ChartOptions<'pie'> = {
        ...this.baseNutrientsChartOptions
    };

    public barChartOptions: ChartOptions<'bar'> = {
        ...this.baseNutrientsChartOptions
    }

    public radarChartOptions: ChartOptions<'radar'> = {
        ...this.baseNutrientsChartOptions
    }

    public caloriesLineChartData = computed<ChartData<'line', number[], string>>(() => {
        const stats = this.chartStatisticsData();
        return {
            labels: stats?.date.map(date => this.getDate(date, this.computedRange().from)) || [],
            datasets: [
                {
                    data: stats?.calories || [],
                    fill: false,
                    tension: 0.5,
                    borderColor: CHART_COLORS.calories,
                }
            ],
        };
    });

    public nutrientsLineChartData = computed<ChartData<'line', number[], string>>(() => {
        const stats = this.chartStatisticsData();
        return {
            labels: stats?.date.map(date => this.getDate(date, this.computedRange().from)) || [],
            datasets: [
                {
                    data: stats?.nutrientsStatistic.proteins || [],
                    label: this.translateService.instant('STATISTICS.NUTRIENTS.PROTEINS'),
                    borderColor: CHART_COLORS.proteins,
                    backgroundColor: CHART_COLORS.proteins,
                    fill: false,
                    tension: 0.5,
                },
                {
                    data: stats?.nutrientsStatistic.fats || [],
                    label: this.translateService.instant('STATISTICS.NUTRIENTS.FATS'),
                    borderColor: CHART_COLORS.fats,
                    backgroundColor: CHART_COLORS.fats,
                    fill: false,
                    tension: 0.5,
                },
                {
                    data: stats?.nutrientsStatistic.carbs || [],
                    label: this.translateService.instant('STATISTICS.NUTRIENTS.CARBS'),
                    borderColor: CHART_COLORS.carbs,
                    backgroundColor: CHART_COLORS.carbs,
                    fill: false,
                    tension: 0.5,
                },
            ],
        };
    });

    public nutrientsPieChartData = computed(() => {
        const aggregatedNutrients = this.chartStatisticsData()?.aggregatedNutrients;

        return {
            labels: [
                this.translateService.instant('NUTRIENTS.PROTEINS'),
                this.translateService.instant('NUTRIENTS.FATS'),
                this.translateService.instant('NUTRIENTS.CARBS'),
            ],
            datasets: [
                {
                    data: [
                        aggregatedNutrients?.proteins,
                        aggregatedNutrients?.fats,
                        aggregatedNutrients?.carbs,
                    ],
                    backgroundColor: [
                        CHART_COLORS.proteins,
                        CHART_COLORS.fats,
                        CHART_COLORS.carbs
                    ],
                },
            ],
        };
    });

    public nutrientsBarChartData = computed(() => {
        const aggregatedNutrients = this.chartStatisticsData()?.aggregatedNutrients;

        return {
            labels: [
                this.translateService.instant('NUTRIENTS.PROTEINS'),
                this.translateService.instant('NUTRIENTS.FATS'),
                this.translateService.instant('NUTRIENTS.CARBS'),
            ],
            datasets: [
                {
                    data: [
                        aggregatedNutrients?.proteins,
                        aggregatedNutrients?.fats,
                        aggregatedNutrients?.carbs,
                    ],
                    backgroundColor: [
                        CHART_COLORS.proteins,
                        CHART_COLORS.fats,
                        CHART_COLORS.carbs
                    ],
                },
            ],
        };
    });

    public nutrientsRadarChartData = computed(() => {
        const aggregatedNutrients = this.chartStatisticsData()?.aggregatedNutrients;

        return {
            labels: [
                this.translateService.instant('NUTRIENTS.PROTEINS'),
                this.translateService.instant('NUTRIENTS.FATS'),
                this.translateService.instant('NUTRIENTS.CARBS'),
            ],
            datasets: [
                {
                    data: [
                        aggregatedNutrients?.proteins,
                        aggregatedNutrients?.fats,
                        aggregatedNutrients?.carbs,
                    ],
                    backgroundColor: CHART_COLORS.radarBackground,
                    borderColor: CHART_COLORS.radarBorder,
                    borderWidth: 2,
                },
            ],
        };
    });

    private getDate(day: TuiDay | number, date: TuiDay): string {
        const actualDay = day instanceof TuiDay ? day : date.append({day});
        const months = this.months();

        return `${months[actualDay.month]}, ${actualDay.day}`;
    }

    private computeRange(range: TuiDayRange): TuiDayRange {
        const {from, to} = range;
        const length = TuiDay.lengthBetween(from, to);
        const dayOfWeekFrom = from.dayOfWeek();
        const dayOfWeekTo = to.dayOfWeek();
        const mondayFrom = dayOfWeekFrom ? from.append({day: 7 - dayOfWeekFrom}) : from;
        const mondayTo = dayOfWeekTo ? to.append({day: 7 - dayOfWeekTo}) : to;
        const mondaysLength = TuiDay.lengthBetween(mondayFrom, mondayTo);

        if (length > 90) {
            return range;
        }

        if (length > 60) {
            return new TuiDayRange(mondayFrom, mondayTo.append({day: mondaysLength % 14}));
        }

        if (length > 14) {
            return new TuiDayRange(mondayFrom, mondayTo);
        }

        return new TuiDayRange(from, to.append({day: length % 2}));
    }

    private getFormattedTooltip<T extends keyof ChartTypeRegistry>(context: TooltipItem<T>, key: string): string {
        const label = context.label || '';
        const value = Number(context.raw) || 0;
        const formattedValue = parseFloat(value.toFixed(2));

        return `${label}: ${formattedValue} ${this.translateService.instant(key)}`;
    }
}

export type RangeMode = 'Week' | 'Month' | 'Year' | 'Custom';

interface StatisticsFormValues {
    range: RangeMode;
    inputRange: TuiDayRange | null;
}

export type StatisticsFormData = FormGroupControls<StatisticsFormValues>;
