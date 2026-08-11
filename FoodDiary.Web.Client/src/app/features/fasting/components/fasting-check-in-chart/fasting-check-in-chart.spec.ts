import { TestBed } from '@angular/core/testing';
import { TranslateService } from '@ngx-translate/core';
import { beforeEach, describe, expect, it, vi } from 'vitest';

import { LocalizationService } from '../../../../shared/i18n/localization.service';
import type { FastingCheckIn } from '../../models/fasting.data';
import { FastingCheckInChartComponent } from './fasting-check-in-chart';

const EARLIER_HUNGER_LEVEL = 2;
const EARLIER_ENERGY_LEVEL = 4;
const EARLIER_MOOD_LEVEL = 3;
const LATER_HUNGER_LEVEL = 3;
const LATER_ENERGY_LEVEL = 2;
const LATER_MOOD_LEVEL = 4;
const CHECK_IN_METRIC_COUNT = 3;
const QUARTER_POSITION = 0.25;

describe('FastingCheckInChartComponent', () => {
    beforeEach(async () => {
        await TestBed.configureTestingModule({
            imports: [FastingCheckInChartComponent],
            providers: [
                {
                    provide: TranslateService,
                    useValue: { instant: vi.fn((key: string) => key) },
                },
                {
                    provide: LocalizationService,
                    useValue: { getCurrentLanguage: vi.fn(() => 'ru') },
                },
            ],
        })
            .overrideComponent(FastingCheckInChartComponent, { set: { template: '<div></div>' } })
            .compileComponents();
    });

    it('sorts check-ins and renders hunger, energy, and mood in one series collection', () => {
        const fixture = TestBed.createComponent(FastingCheckInChartComponent);
        fixture.componentRef.setInput('checkIns', createCheckIns());

        const series = fixture.componentInstance['series']();

        expect(series).toHaveLength(CHECK_IN_METRIC_COUNT);
        expect(series.map(item => item.label)).toEqual(['FASTING.CHECK_IN.HUNGER', 'FASTING.CHECK_IN.ENERGY', 'FASTING.CHECK_IN.MOOD']);
        expect(series[0]?.points.map(point => point.value)).toEqual([EARLIER_HUNGER_LEVEL, LATER_HUNGER_LEVEL]);
        expect(series[1]?.points.map(point => point.value)).toEqual([EARLIER_ENERGY_LEVEL, LATER_ENERGY_LEVEL]);
        expect(series[2]?.points.map(point => point.value)).toEqual([EARLIER_MOOD_LEVEL, LATER_MOOD_LEVEL]);
    });

    it('positions check-ins proportionally to elapsed time', () => {
        const fixture = TestBed.createComponent(FastingCheckInChartComponent);
        fixture.componentRef.setInput('checkIns', [
            createCheckIn('2026-04-12T11:00:00Z'),
            createCheckIn('2026-04-12T11:15:00Z'),
            createCheckIn('2026-04-12T12:00:00Z'),
        ]);

        const points = fixture.componentInstance['series']()[0]?.points ?? [];

        expect(points.map(point => point.xPosition)).toEqual([0, QUARTER_POSITION, 1]);
    });
});

function createCheckIn(checkedInAtUtc: string): FastingCheckIn {
    return {
        id: checkedInAtUtc,
        checkedInAtUtc,
        hungerLevel: EARLIER_HUNGER_LEVEL,
        energyLevel: EARLIER_ENERGY_LEVEL,
        moodLevel: EARLIER_MOOD_LEVEL,
        symptoms: [],
        notes: null,
    };
}

function createCheckIns(): FastingCheckIn[] {
    return [
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
    ];
}
