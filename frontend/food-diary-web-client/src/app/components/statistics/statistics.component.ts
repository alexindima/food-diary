import {
    ChangeDetectionStrategy,
    Component,
    computed,
    HostListener,
    inject,
    OnInit,
    signal
} from '@angular/core';
import { AggregatedStatistics, CaloriesChartData, NutrientsChartData, PieChartData, StatisticsMapper } from '../../types/statistics.data';
import { StatisticsService } from '../../services/statistics.service';
import { DecimalPipe } from '@angular/common';
import { TUI_MONTHS, TuiGroup, TuiHintOptionsDirective, TuiLoader, TuiPoint } from '@taiga-ui/core';
import {
    TuiAxes,
    TuiLineChart,
    TuiLineDaysChart,
    TuiLineDaysChartHint,
    TuiPieChart
} from '@taiga-ui/addon-charts';
import {
    TuiDay,
    TuiDayLike,
    TuiDayRange, TuiFilterPipe, TuiMapper, TuiMapperPipe,
    TuiMatcher,
    TuiMonth,
    tuiPure,
    TuiStringHandler
} from '@taiga-ui/cdk';
import { map, Observable } from 'rxjs';
import { TuiInputDateRangeModule } from '@taiga-ui/legacy';
import { FormControl, FormGroup, FormsModule, ReactiveFormsModule } from '@angular/forms';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { TuiBlock } from '@taiga-ui/kit';
import { FormGroupControls } from '../../types/common.data';

@Component({
    selector: 'app-statistics',
    imports: [
        TuiAxes,
        TuiLineDaysChart,
        TuiLineDaysChartHint,
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
        TuiFilterPipe,
        TuiLineChart,
        TuiMapperPipe,
    ],
    templateUrl: './statistics.component.html',
    styleUrls: ['./statistics.component.less'],
    changeDetection: ChangeDetectionStrategy.OnPush
})
export class StatisticsComponent implements OnInit {
    protected readonly maxLength: TuiDayLike = { year: 1 };
    private readonly defaultRange: RangeMode = 'Month';

    private readonly statisticsService = inject(StatisticsService);
    private readonly translateService = inject(TranslateService);
    private readonly months$ = inject(TUI_MONTHS);

    public isLoading = signal<boolean>(false);
    public isMobile = signal<boolean>(false);

    public caloriesChartData = signal<CaloriesChartData | null>(null);
    public nutrientsDaysChartData = signal<NutrientsChartData | null>(null);
    public nutrientsPieChartData = signal<PieChartData | null>(null);
    public data = signal(new TuiDayRange(TuiDay.currentLocal().append({ month: -1 }), TuiDay.currentLocal()));
    private readonly monthsArray = signal<string[]>([]);

    protected readonly rangeForm: FormGroup<StatisticsFormData> = new FormGroup({
        range: new FormControl<RangeMode>(this.defaultRange, { nonNullable: true }),
        inputRange: new FormControl<TuiDayRange | null>(
            new TuiDayRange(TuiDay.currentLocal().append({ month: -1 }), TuiDay.currentLocal()),
            { nonNullable: false },
        ),
    });

    @tuiPure
    protected getWidth({from, to}: TuiDayRange): number {
        return TuiDay.lengthBetween(from, to);
    }

    protected readonly filter: TuiMatcher<[readonly [TuiDay, number], TuiDayRange]> = (
        [day],
        {from, to},
    ) => day.daySameOrAfter(from) && day.daySameOrBefore(to);

    protected readonly toNumbers: TuiMapper<
        [ReadonlyArray<readonly [TuiDay, number]>, TuiDayRange],
        readonly TuiPoint[]
    > = (days, {from}) =>
        days.map(([day, value]) => [TuiDay.lengthBetween(from, day), value]);

    protected readonly xStringify = computed<TuiStringHandler<TuiDay>>(() => {
        const months = this.monthsArray();
        return ({ month, day }): string => `${months[month]}, ${day}`;
    });

    protected readonly yStringify: TuiStringHandler<number> = y => `${Number(y.toFixed(2))} ${this.translateService.instant('kkal')}`;

    @HostListener('window:resize', [])
    public onResize(): void {
        this.checkScreenWidth();
    }

    public constructor() {
        this.updateDateRange(this.defaultRange);

        this.months$.subscribe(months => {
            const monthsArray = Object.values(months);
            this.monthsArray.set(monthsArray);
        });
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
                this.data.set(inputRange);
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
                dateFrom: this.data().from.toLocalNativeDate(),
                dateTo: this.data().to.toLocalNativeDate(),
            })
            .subscribe({
                next: response => {
                    if (response.status === 'success') {
                        const stats: AggregatedStatistics[] = response.data!;
                        this.caloriesChartData.set(StatisticsMapper.mapCaloriesToChartData(stats));
                        this.nutrientsDaysChartData.set(StatisticsMapper.mapNutrientsToDaysChartData(stats));
                        this.nutrientsPieChartData.set(StatisticsMapper.mapNutrientsToPieChartData(stats));
                    }
                    this.isLoading.set(false);
                },
                error: () => {
                    this.isLoading.set(false);
                },
            });
    }

    protected get range(): TuiDayRange {
        return this.computeRange(this.data());
    }

    @tuiPure
    protected getDate(day: TuiDay | number, date: TuiDay): string {
        const actualDay = day instanceof TuiDay ? day : date.append({ day });

        const xStringify = this.xStringify();

        return xStringify(actualDay);
    }

    public labels = computed(() => {
        const months = this.monthsArray();

        const { from, to } = this.data();
        const length = TuiDay.lengthBetween(from, to);

        let result: string[];

        if (length > 90) {
            result = Array.from(
                { length: TuiMonth.lengthBetween(from, to) + 1 },
                (_, i) => months[from.append({ month: i }).month] ?? '',
            );
        } else {
            const range = Array.from({ length }, (_, day) => from.append({ day }));
            const mondays = onlyMondays(range);
            const days = range.map(String);

            if (length > 60) {
                result = [...even(mondays), ''];
            } else if (length > 14) {
                result = [...mondays, ''];
            } else if (length > 7) {
                result = [...even(days), ''];
            } else {
                result = [...days, ''];
            }
        }

        return result;
    });

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
                this.data.set(new TuiDayRange(now.append({ day: -7 }), now));
                break;
            case 'Month':
                this.data.set(new TuiDayRange(now.append({ month: -1 }), now));
                break;
            case 'Year':
                this.data.set( new TuiDayRange(now.append({ year: -1 }), now));
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
