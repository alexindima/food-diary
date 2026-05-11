import { TestBed } from '@angular/core/testing';
import { TranslateService } from '@ngx-translate/core';
import { FD_UI_DIALOG_DATA } from 'fd-ui-kit/dialog/fd-ui-dialog-data';
import { beforeEach, describe, expect, it, vi } from 'vitest';

import { LocalizationService } from '../../../../services/localization.service';
import { FastingCheckInChartDialogComponent } from './fasting-checkin-chart-dialog.component';

const EARLIER_HUNGER_LEVEL = 2;
const EARLIER_ENERGY_LEVEL = 4;
const EARLIER_MOOD_LEVEL = 3;
const LATER_HUNGER_LEVEL = 3;
const LATER_ENERGY_LEVEL = 2;
const LATER_MOOD_LEVEL = 4;
const CHECK_IN_METRIC_COUNT = 3;
const CHECK_IN_SCALE_MAX = 5;

describe('FastingCheckInChartDialogComponent', () => {
    beforeEach(async () => {
        await TestBed.configureTestingModule({
            imports: [FastingCheckInChartDialogComponent],
            providers: [
                {
                    provide: FD_UI_DIALOG_DATA,
                    useValue: {
                        title: 'Chart title',
                        subtitle: 'Chart subtitle',
                        checkIns: [
                            {
                                id: 'checkin-2',
                                checkedInAtUtc: '2026-04-12T12:30:00Z',
                                hungerLevel: LATER_HUNGER_LEVEL,
                                energyLevel: LATER_ENERGY_LEVEL,
                                moodLevel: LATER_MOOD_LEVEL,
                                symptoms: ['weakness'],
                                notes: 'later',
                            },
                            {
                                id: 'checkin-1',
                                checkedInAtUtc: '2026-04-12T11:00:00Z',
                                hungerLevel: EARLIER_HUNGER_LEVEL,
                                energyLevel: EARLIER_ENERGY_LEVEL,
                                moodLevel: EARLIER_MOOD_LEVEL,
                                symptoms: ['headache'],
                                notes: 'earlier',
                            },
                        ],
                    },
                },
                {
                    provide: TranslateService,
                    useValue: {
                        instant: vi.fn((key: string, params?: Record<string, string>) => {
                            if (params === undefined) {
                                return key;
                            }

                            return `${key}:${Object.values(params).join('|')}`;
                        }),
                    },
                },
                {
                    provide: LocalizationService,
                    useValue: {
                        getCurrentLanguage: vi.fn(() => 'ru'),
                    },
                },
            ],
        })
            .overrideComponent(FastingCheckInChartDialogComponent, {
                set: {
                    template: '<div></div>',
                },
            })
            .compileComponents();
    });

    it('sorts points chronologically and builds datasets for three metrics', () => {
        const fixture = TestBed.createComponent(FastingCheckInChartDialogComponent);
        const component = fixture.componentInstance;

        const points = component.points();
        const chartData = component.chartData();

        expect(points.map(point => point.checkedInAtUtc)).toEqual(['2026-04-12T11:00:00Z', '2026-04-12T12:30:00Z']);
        expect(chartData.datasets).toHaveLength(CHECK_IN_METRIC_COUNT);
        expect(chartData.datasets[0]?.label).toBe('FASTING.CHECK_IN.HUNGER');
        expect(chartData.datasets[1]?.label).toBe('FASTING.CHECK_IN.ENERGY');
        expect(chartData.datasets[2]?.label).toBe('FASTING.CHECK_IN.MOOD');
        expect(chartData.datasets[0]?.data).toEqual([EARLIER_HUNGER_LEVEL, LATER_HUNGER_LEVEL]);
        expect(chartData.datasets[1]?.data).toEqual([EARLIER_ENERGY_LEVEL, LATER_ENERGY_LEVEL]);
        expect(chartData.datasets[2]?.data).toEqual([EARLIER_MOOD_LEVEL, LATER_MOOD_LEVEL]);
    });

    it('configures fixed 1..5 y-axis and tooltip footer from symptoms and notes', () => {
        const fixture = TestBed.createComponent(FastingCheckInChartDialogComponent);
        const component = fixture.componentInstance;

        expectFixedYAxis(component);
        expectTooltipFooter(component);
    });
});

function expectFixedYAxis(component: FastingCheckInChartDialogComponent): void {
    const yScale = component.chartOptions?.scales?.['y'];
    const yTicks = yScale?.ticks as { stepSize?: number } | undefined;

    expect(yScale?.min).toBe(1);
    expect(yScale?.max).toBe(CHECK_IN_SCALE_MAX);
    expect(yTicks?.stepSize).toBe(1);
}

function expectTooltipFooter(component: FastingCheckInChartDialogComponent): void {
    const tooltipCallbacks = component.chartOptions?.plugins?.tooltip?.callbacks;
    const tooltipArgs = [{ dataIndex: 0 }];
    const titleCallback = tooltipCallbacks?.title as ((items: unknown[]) => string | string[]) | undefined;
    const footerCallback = tooltipCallbacks?.footer as ((items: unknown[]) => string | string[]) | undefined;
    const title = titleCallback === undefined ? '' : Reflect.apply(titleCallback, undefined, [tooltipArgs]);
    const footer = footerCallback === undefined ? '' : Reflect.apply(footerCallback, undefined, [tooltipArgs]);

    expect(title).toContain('2026');
    expect(String(footer)).toContain('FASTING.CHECK_IN.CHART_TOOLTIP_SYMPTOMS');
    expect(String(footer)).toContain('FASTING.CHECK_IN.CHART_TOOLTIP_NOTES');
}
