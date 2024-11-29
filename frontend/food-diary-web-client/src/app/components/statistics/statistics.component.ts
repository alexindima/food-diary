import { ChangeDetectionStrategy, Component, HostListener, inject, OnInit, signal } from '@angular/core';
import { AggregatedStatistics, CaloriesChartData, NutrientsChartData, PieChartData, StatisticsMapper } from '../../types/statistics.data';
import { StatisticsService } from '../../services/statistics.service';
import { AsyncPipe, DecimalPipe } from '@angular/common';
import { TUI_MONTHS, TuiGroup, TuiHintOptionsDirective, TuiLoader } from '@taiga-ui/core';
import { TuiAxes, TuiLineDaysChart, TuiLineDaysChartHint, TuiPieChart } from '@taiga-ui/addon-charts';
import { TuiDay, TuiDayLike, TuiDayRange, TuiMonth, tuiPure, TuiStringHandler } from '@taiga-ui/cdk';
import { map, Observable, of } from 'rxjs';
import { TuiInputDateRangeModule } from '@taiga-ui/legacy';
import { FormControl, FormGroup, FormsModule, ReactiveFormsModule } from '@angular/forms';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { TuiBlock } from '@taiga-ui/kit';
import { FormGroupControls } from '../../types/common.data';

@Component({
    selector: 'app-statistics',
    standalone: true,
    imports: [
        TuiAxes,
        TuiLineDaysChart,
        TuiLineDaysChartHint,
        AsyncPipe,
        TuiInputDateRangeModule,
        FormsModule,
        TuiLoader,
        TuiPieChart,
        TuiHintOptionsDirective,
        TranslatePipe,
        DecimalPipe,
        TuiGroup,
        ReactiveFormsModule,
        TuiBlock,
    ],
    templateUrl: './statistics.component.html',
    styleUrls: ['./statistics.component.less'],
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class StatisticsComponent implements OnInit {
    protected readonly maxLength: TuiDayLike = { year: 1 };
    private readonly defaultRange: RangeMode = 'Month';

    private readonly statisticsService = inject(StatisticsService);
    private readonly translateService = inject(TranslateService);
    private readonly months$ = inject(TUI_MONTHS);

    public isLoading = signal<boolean>(false);
    public isMobile = signal<boolean>(false);

    public caloriesChartData: CaloriesChartData | null = null;
    public nutrientsDaysChartData: NutrientsChartData | null = null;
    public nutrientsPieChartData: PieChartData | null = null;
    public data = new TuiDayRange(TuiDay.currentLocal().append({ month: -1 }), TuiDay.currentLocal());
    public labels$: Observable<readonly string[]> = this.labels(this.data);

    protected readonly rangeForm: FormGroup<StatisticsFormData> = new FormGroup({
        range: new FormControl<RangeMode>(this.defaultRange, { nonNullable: true }),
        inputRange: new FormControl<TuiDayRange | null>(
            new TuiDayRange(TuiDay.currentLocal().append({ month: -1 }), TuiDay.currentLocal()),
            { nonNullable: false },
        ),
    });

    protected readonly xStringify$: Observable<TuiStringHandler<TuiDay>> = this.months$.pipe(
        map(
            months =>
                ({ month, day }) =>
                    `${months[month]}, ${day}`,
        ),
    );

    protected readonly yStringify: TuiStringHandler<number> = y => `${Number(y.toFixed(2))} ${this.translateService.instant('kkal')}`;

    @HostListener('window:resize', [])
    public onResize(): void {
        this.checkScreenWidth();
    }

    public constructor() {
        this.updateDateRange(this.defaultRange);
    }

    public ngOnInit(): void {
        this.checkScreenWidth();

        this.rangeForm.controls.range.valueChanges.subscribe(range => {
            if (range) {
                this.updateDateRange(range);
            }
        });

        this.rangeForm.controls.inputRange.valueChanges.subscribe(inputRange => {
            if (this.rangeForm.controls.range.value === 'Custom' && inputRange) {
                this.data = inputRange;
                this.updateCharts();
            }
        });
    }

    private checkScreenWidth(): void {
        this.isMobile.set(window.innerWidth <= 600);
    }

    @tuiPure
    protected computeLabels$({ from, to }: TuiDayRange): Observable<ReadonlyArray<string | null>> {
        return this.months$.pipe(
            map(months => [
                ...Array.from({ length: TuiMonth.lengthBetween(from, to) + 1 }, (_, i) => months[from.append({ month: i }).month] ?? ''),
                null,
            ]),
        );
    }

    private updateCharts(): void {
        this.isLoading.set(true);

        this.statisticsService
            .getAggregatedStatistics({
                dateFrom: this.data.from.toLocalNativeDate(),
                dateTo: this.data.to.toLocalNativeDate(),
            })
            .subscribe({
                next: response => {
                    if (response.status === 'success') {
                        const stats: AggregatedStatistics[] = response.data!;
                        this.caloriesChartData = StatisticsMapper.mapCaloriesToChartData(stats);
                        this.nutrientsDaysChartData = StatisticsMapper.mapNutrientsToDaysChartData(stats);
                        this.nutrientsPieChartData = StatisticsMapper.mapNutrientsToPieChartData(stats);
                        this.labels$ = this.labels(this.data);
                    }
                    this.isLoading.set(false);
                },
                error: () => {
                    this.isLoading.set(false);
                },
            });
    }

    protected get range(): TuiDayRange {
        return this.computeRange(this.data);
    }

    @tuiPure
    protected getDate(day: TuiDay | number, date: TuiDay): string {
        const actualDay = day instanceof TuiDay ? day : date.append({ day });
        const xStringify = this.xStringify$;

        let formattedDate = '';
        xStringify
            .subscribe(handler => {
                formattedDate = handler(actualDay);
            })
            .unsubscribe();

        return formattedDate;
    }

    @tuiPure
    protected labels({ from, to }: TuiDayRange): Observable<readonly string[]> {
        const length = TuiDay.lengthBetween(from, to);

        if (length > 90) {
            return this.months$.pipe(
                map(months => [
                    ...Array.from(
                        { length: TuiMonth.lengthBetween(from, to) + 1 },
                        (_, i) => months[from.append({ month: i }).month] ?? '',
                    ),
                    '',
                ]),
            );
        }

        const range = Array.from({ length }, (_, day) => from.append({ day }));
        const mondays = onlyMondays(range);
        const days = range.map(String);

        if (length > 60) {
            return of([...even(mondays), '']);
        }

        if (length > 14) {
            return of([...mondays, '']);
        }

        if (length > 7) {
            return of([...even(days), '']);
        }

        return of([...days, '']);
    }

    @tuiPure
    private computeRange(range: TuiDayRange): TuiDayRange {
        const { from, to } = range;
        const length = TuiDay.lengthBetween(from, to);
        const dayOfWeekFrom = from.dayOfWeek();
        const dayOfWeekTo = to.dayOfWeek();
        const mondayFrom = dayOfWeekFrom ? from.append({ day: 7 - dayOfWeekFrom }) : from;
        const mondayTo = dayOfWeekTo ? to.append({ day: 7 - dayOfWeekTo }) : to;
        const mondaysLength = TuiDay.lengthBetween(mondayFrom, mondayTo);

        if (length > 90) {
            return range;
        }

        if (length > 60) {
            return new TuiDayRange(mondayFrom, mondayTo.append({ day: mondaysLength % 14 }));
        }

        if (length > 14) {
            return new TuiDayRange(mondayFrom, mondayTo);
        }

        return new TuiDayRange(from, to.append({ day: length % 2 }));
    }

    private updateDateRange(range: RangeMode): void {
        const now = TuiDay.currentLocal();
        switch (range) {
            case 'Week':
                this.data = new TuiDayRange(now.append({ day: -7 }), now);
                break;
            case 'Month':
                this.data = new TuiDayRange(now.append({ month: -1 }), now);
                break;
            case 'Year':
                this.data = new TuiDayRange(now.append({ year: -1 }), now);
                break;
            case 'Custom':
                const inputRange = this.rangeForm.controls.inputRange.value;
                if (inputRange) {
                    this.data = new TuiDayRange(inputRange.from, inputRange.to);
                }
                break;
            default:
                break;
        }

        this.updateCharts();
    }
}

function onlyMondays(range: readonly TuiDay[]): readonly string[] {
    return range.filter(day => !day.dayOfWeek()).map(String);
}

function even<T>(array: readonly T[]): readonly T[] {
    return array.filter((_, i) => !(i % 2));
}

export type RangeMode = 'Week' | 'Month' | 'Year' | 'Custom';

interface StatisticsFormValues {
    range: RangeMode;
    inputRange: TuiDayRange | null;
}

export type StatisticsFormData = FormGroupControls<StatisticsFormValues>;
