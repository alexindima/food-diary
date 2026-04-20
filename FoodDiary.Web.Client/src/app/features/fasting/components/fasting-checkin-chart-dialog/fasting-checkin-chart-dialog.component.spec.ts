import { beforeEach, describe, expect, it, vi } from 'vitest';
import { TestBed } from '@angular/core/testing';
import { TranslateService } from '@ngx-translate/core';
import { FD_UI_DIALOG_DATA } from 'fd-ui-kit/dialog/fd-ui-dialog-data';
import { LocalizationService } from '../../../../services/localization.service';
import { FastingCheckInChartDialogComponent } from './fasting-checkin-chart-dialog.component';

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
                                hungerLevel: 3,
                                energyLevel: 2,
                                moodLevel: 4,
                                symptoms: ['weakness'],
                                notes: 'later',
                            },
                            {
                                id: 'checkin-1',
                                checkedInAtUtc: '2026-04-12T11:00:00Z',
                                hungerLevel: 2,
                                energyLevel: 4,
                                moodLevel: 3,
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
                            if (!params) {
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
        expect(chartData.datasets).toHaveLength(3);
        expect(chartData.datasets[0]?.label).toBe('FASTING.CHECK_IN.HUNGER');
        expect(chartData.datasets[1]?.label).toBe('FASTING.CHECK_IN.ENERGY');
        expect(chartData.datasets[2]?.label).toBe('FASTING.CHECK_IN.MOOD');
        expect(chartData.datasets[0]?.data).toEqual([2, 3]);
        expect(chartData.datasets[1]?.data).toEqual([4, 2]);
        expect(chartData.datasets[2]?.data).toEqual([3, 4]);
    });

    it('configures fixed 1..5 y-axis and tooltip footer from symptoms and notes', () => {
        const fixture = TestBed.createComponent(FastingCheckInChartDialogComponent);
        const component = fixture.componentInstance;
        const tooltipCallbacks = component.chartOptions?.plugins?.tooltip?.callbacks;
        const yScale = component.chartOptions?.scales?.['y'];
        const yTicks = yScale?.ticks as { stepSize?: number } | undefined;

        expect(yScale?.min).toBe(1);
        expect(yScale?.max).toBe(5);
        expect(yTicks?.stepSize).toBe(1);

        const tooltipArgs = [{ dataIndex: 0 }];
        const titleCallback = tooltipCallbacks?.title as ((items: unknown[]) => string | string[]) | undefined;
        const footerCallback = tooltipCallbacks?.footer as ((items: unknown[]) => string | string[]) | undefined;
        const title = titleCallback ? Reflect.apply(titleCallback, undefined, [tooltipArgs]) : '';
        const footer = footerCallback ? Reflect.apply(footerCallback, undefined, [tooltipArgs]) : '';

        expect(title).toContain('2026');
        expect(String(footer)).toContain('FASTING.CHECK_IN.CHART_TOOLTIP_SYMPTOMS');
        expect(String(footer)).toContain('FASTING.CHECK_IN.CHART_TOOLTIP_NOTES');
    });
});
