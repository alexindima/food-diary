import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { By } from '@angular/platform-browser';
import { TranslateService } from '@ngx-translate/core';
import { FdUiLineChartComponent } from 'fd-ui-kit';
import { firstValueFrom } from 'rxjs';
import { beforeEach, describe, expect, it } from 'vitest';

import { provideTranslateTesting } from '../../../../../testing/translate-testing.module';
import { LocalizationService } from '../../../../shared/i18n/localization.service';
import type { FastingCheckIn } from '../../../../shared/models/fasting.data';
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
                provideTranslateTesting(),
                {
                    provide: LocalizationService,
                    useValue: { getCurrentLanguage: (): string => TestBed.inject(TranslateService).getCurrentLang() ?? 'en' },
                },
            ],
        }).compileComponents();
    });

    it('sorts check-ins and renders hunger, energy, and mood in one series collection', () => {
        const fixture = renderCheckIns(createCheckIns());
        const series = lineChart(fixture).series();

        expect(series).toHaveLength(CHECK_IN_METRIC_COUNT);
        expect(series.map(item => item.label)).toEqual(['FASTING.CHECK_IN.HUNGER', 'FASTING.CHECK_IN.ENERGY', 'FASTING.CHECK_IN.MOOD']);
        expect(series[0]?.points.map(point => point.value)).toEqual([EARLIER_HUNGER_LEVEL, LATER_HUNGER_LEVEL]);
        expect(series[1]?.points.map(point => point.value)).toEqual([EARLIER_ENERGY_LEVEL, LATER_ENERGY_LEVEL]);
        expect(series[2]?.points.map(point => point.value)).toEqual([EARLIER_MOOD_LEVEL, LATER_MOOD_LEVEL]);
    });

    it('positions check-ins proportionally to elapsed time', () => {
        const fixture = renderCheckIns([
            createCheckIn('2026-04-12T11:00:00Z'),
            createCheckIn('2026-04-12T11:15:00Z'),
            createCheckIn('2026-04-12T12:00:00Z'),
        ]);

        const points = lineChart(fixture).series()[0]?.points ?? [];

        expect(points.map(point => point.xPosition)).toEqual([0, QUARTER_POSITION, 1]);
    });

    it('updates the rendered legend and tooltip when the language changes without dropping any check-ins', async () => {
        const translate = TestBed.inject(TranslateService);
        translate.setTranslation('en', { FASTING: { CHECK_IN: { HUNGER: 'Hunger', ENERGY: 'Energy', MOOD: 'Mood' } } });
        translate.setTranslation('ru', { FASTING: { CHECK_IN: { HUNGER: 'Голод', ENERGY: 'Энергия', MOOD: 'Настроение' } } });
        await firstValueFrom(translate.use('en'));
        const checkIns = createCheckIns();
        const fixture = renderCheckIns(checkIns);
        const host = fixture.nativeElement as HTMLElement;
        const points = host.querySelectorAll<HTMLElement>('.fd-ui-line-chart__point');
        expect(points).toHaveLength(checkIns.length * CHECK_IN_METRIC_COUNT);
        expect(points[0].title).toContain('Hunger');
        expect(host.querySelector('.fd-ui-line-chart__legend')?.textContent).toContain('Energy');

        await firstValueFrom(translate.use('ru'));
        fixture.detectChanges();

        const localized = host.querySelectorAll<HTMLElement>('.fd-ui-line-chart__point');
        expect(localized).toHaveLength(points.length);
        expect(localized[0].title).toContain('Голод');
        expect(host.querySelector('.fd-ui-line-chart__legend')?.textContent).toContain('Энергия');
        expect(
            lineChart(fixture)
                .series()[0]
                ?.points.map(point => point.value),
        ).toEqual([EARLIER_HUNGER_LEVEL, LATER_HUNGER_LEVEL]);
    });
});

function renderCheckIns(checkIns: FastingCheckIn[]): ComponentFixture<FastingCheckInChartComponent> {
    const fixture = TestBed.createComponent(FastingCheckInChartComponent);
    fixture.componentRef.setInput('checkIns', checkIns);
    fixture.detectChanges();
    return fixture;
}

function lineChart(fixture: ComponentFixture<FastingCheckInChartComponent>): FdUiLineChartComponent {
    return fixture.debugElement.query(By.directive(FdUiLineChartComponent)).injector.get(FdUiLineChartComponent);
}

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
