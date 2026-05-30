import { TestBed } from '@angular/core/testing';
import { TranslateService } from '@ngx-translate/core';
import { FD_UI_DIALOG_DATA } from 'fd-ui-kit/dialog/fd-ui-dialog-data';
import { beforeEach, describe, expect, it, vi } from 'vitest';

import { LocalizationService } from '../../../../shared/i18n/localization.service';
import { FastingCheckInChartDialogComponent } from './fasting-checkin-chart-dialog';

const EARLIER_HUNGER_LEVEL = 2;
const EARLIER_ENERGY_LEVEL = 4;
const EARLIER_MOOD_LEVEL = 3;
const LATER_HUNGER_LEVEL = 3;
const LATER_ENERGY_LEVEL = 2;
const LATER_MOOD_LEVEL = 4;
const CHECK_IN_METRIC_COUNT = 3;

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

    it('sorts points chronologically and builds series for three metrics', () => {
        const fixture = TestBed.createComponent(FastingCheckInChartDialogComponent);
        const component = fixture.componentInstance;

        const points = component['points']();
        const chartSeries = component['chartSeries']();

        expect(points.map(point => point.checkedInAtUtc)).toEqual(['2026-04-12T11:00:00Z', '2026-04-12T12:30:00Z']);
        expect(chartSeries).toHaveLength(CHECK_IN_METRIC_COUNT);
        expect(chartSeries[0]?.label).toBe('FASTING.CHECK_IN.HUNGER');
        expect(chartSeries[1]?.label).toBe('FASTING.CHECK_IN.ENERGY');
        expect(chartSeries[2]?.label).toBe('FASTING.CHECK_IN.MOOD');
        expect(chartSeries[0]?.points.map(point => point.value)).toEqual([EARLIER_HUNGER_LEVEL, LATER_HUNGER_LEVEL]);
        expect(chartSeries[1]?.points.map(point => point.value)).toEqual([EARLIER_ENERGY_LEVEL, LATER_ENERGY_LEVEL]);
        expect(chartSeries[2]?.points.map(point => point.value)).toEqual([EARLIER_MOOD_LEVEL, LATER_MOOD_LEVEL]);
    });
});
