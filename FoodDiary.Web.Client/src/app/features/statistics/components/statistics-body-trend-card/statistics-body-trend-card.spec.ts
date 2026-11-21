import { TestBed } from '@angular/core/testing';
import { provideRouter, Router } from '@angular/router';
import { TranslateService } from '@ngx-translate/core';
import { describe, expect, it, vi } from 'vitest';

import { provideTranslateTesting } from '../../../../../testing/translate-testing.module';
import { MeasurementSystemService } from '../../../../shared/measurements/measurement-system.service';
import { StatisticsBodyTrendCardComponent } from './statistics-body-trend-card';

describe('StatisticsBodyTrendCardComponent', () => {
    it('switches between the weight and waist trend, insights, and history action', async () => {
        await TestBed.configureTestingModule({
            imports: [StatisticsBodyTrendCardComponent],
            providers: [provideTranslateTesting(), provideRouter([])],
        }).compileComponents();
        TestBed.inject(MeasurementSystemService).setSystem('metric');
        const fixture = TestBed.createComponent(StatisticsBodyTrendCardComponent);
        fixture.componentRef.setInput('data', {
            weight: {
                key: 'weight',
                current: 113,
                change: -3,
                goal: 75,
                timeframeDays: 30,
                measurementCount: 22,
                points: [
                    { label: '20 Jul', value: 116 },
                    { label: '4 Aug', value: 113 },
                ],
            },
            waist: {
                key: 'waist',
                current: 99,
                change: 2,
                goal: 80,
                timeframeDays: 30,
                measurementCount: 21,
                points: [
                    { label: '20 Jul', value: 97 },
                    { label: '4 Aug', value: 99 },
                ],
            },
        });
        fixture.detectChanges();
        const root = fixture.nativeElement as HTMLElement;

        expect(root.querySelector('fd-ui-line-chart')).not.toBeNull();
        expect(root.querySelector('fd-ui-button')).not.toBeNull();
        expect(root.textContent).toContain('113');
        expect(root.textContent).toContain('−3');
        expect(root.querySelector('.statistics-body-trend-card__change--positive')).not.toBeNull();
        expect(root.querySelectorAll('.statistics-body-trend-card__insight')[3].textContent).toContain('22');

        const translateService = TestBed.inject(TranslateService);
        translateService.use('ru');
        fixture.detectChanges();
        expect(root.querySelector('.fd-ui-line-chart__y-axis')?.textContent).toMatch(/\d,\d/);
        expect(root.querySelector('.fd-ui-line-chart__y-axis')?.textContent).not.toMatch(/\d\.\d/);
        translateService.use('en');
        fixture.detectChanges();
        expect(root.querySelector('.fd-ui-line-chart__y-axis')?.textContent).toMatch(/\d\.\d/);

        const waist = buttonByLabel(root, 'STATISTICS.DASHBOARD.BODY.WAIST');
        waist.click();
        fixture.detectChanges();

        expect(waist.getAttribute('aria-checked')).toBe('true');
        expect(root.querySelector('fd-ui-line-chart')).not.toBeNull();
        expect(root.textContent).toContain('STATISTICS.DASHBOARD.BODY.CURRENT_WAIST');
        expect(root.querySelector('.statistics-body-trend-card__change')?.textContent.trim()).toContain('+2');
        expect(root.querySelector('.statistics-body-trend-card__change')?.textContent.trim()).toContain('GENERAL.UNITS.CM');
        expect(root.querySelector('.statistics-body-trend-card__change--negative')).not.toBeNull();
        expect(root.querySelectorAll('.statistics-body-trend-card__insight')[3].textContent).toContain('21');

        const navigate = vi.spyOn(TestBed.inject(Router), 'navigateByUrl').mockResolvedValue(true);
        buttonByLabel(root, 'STATISTICS.DASHBOARD.BODY.OPEN_WAIST_HISTORY').click();
        expect(navigate).toHaveBeenCalledOnce();
        expect(navigate.mock.calls[0][0].toString()).toBe('/waist-history');
    });
});

describe('StatisticsBodyTrendCard empty metrics', () => {
    it('keeps the empty waist state distinct when the weight chart has data', async () => {
        await TestBed.configureTestingModule({
            imports: [StatisticsBodyTrendCardComponent],
            providers: [provideTranslateTesting(), provideRouter([])],
        }).compileComponents();
        const fixture = TestBed.createComponent(StatisticsBodyTrendCardComponent);
        fixture.componentRef.setInput('data', {
            weight: {
                key: 'weight',
                current: 80,
                change: null,
                goal: null,
                timeframeDays: 7,
                measurementCount: 1,
                points: [{ label: '20 Jul', value: 80 }],
            },
            waist: {
                key: 'waist',
                current: null,
                change: null,
                goal: null,
                timeframeDays: 7,
                measurementCount: 0,
                points: [{ label: '20 Jul', value: null }],
            },
        });
        fixture.detectChanges();
        const root = fixture.nativeElement as HTMLElement;
        expect(root.querySelector('fd-ui-line-chart')).not.toBeNull();
        expect(root.textContent).toContain('STATISTICS.DASHBOARD.BODY.GOAL_EMPTY');

        buttonByLabel(root, 'STATISTICS.DASHBOARD.BODY.WAIST').click();
        fixture.detectChanges();

        expect(root.querySelector('fd-ui-line-chart')).toBeNull();
        expect(root.querySelector('.statistics-body-trend-card__change')).toBeNull();
        expect(root.textContent).toContain('STATISTICS.DASHBOARD.BODY.EMPTY_WAIST');
        expect(root.textContent).toContain('STATISTICS.DASHBOARD.BODY.OPEN_WAIST_HISTORY');
    });
});

describe('StatisticsBodyTrendCard small changes', () => {
    const INITIAL_WEIGHT_KG = 80;
    const SMALL_WEIGHT_INCREASE_KG = 0.04;

    it.each([
        { change: 0, displayed: '0', hasIncreaseClass: false },
        { change: SMALL_WEIGHT_INCREASE_KG, displayed: '+0', hasIncreaseClass: true },
    ])('displays a $change kg weight change with the correct direction', async ({ change, displayed, hasIncreaseClass }) => {
        await TestBed.configureTestingModule({
            imports: [StatisticsBodyTrendCardComponent],
            providers: [provideTranslateTesting(), provideRouter([])],
        }).compileComponents();
        TestBed.inject(MeasurementSystemService).setSystem('metric');
        const fixture = TestBed.createComponent(StatisticsBodyTrendCardComponent);
        fixture.componentRef.setInput('data', {
            weight: {
                key: 'weight',
                current: INITIAL_WEIGHT_KG + change,
                change,
                goal: null,
                timeframeDays: 7,
                measurementCount: 2,
                points: [
                    { label: '20 Jul', value: INITIAL_WEIGHT_KG },
                    { label: '21 Jul', value: INITIAL_WEIGHT_KG + change },
                ],
            },
            waist: { key: 'waist', current: null, change: null, goal: null, timeframeDays: 7, measurementCount: 0, points: [] },
        });
        fixture.detectChanges();

        const root = fixture.nativeElement as HTMLElement;
        const changeLabel = root.querySelector('.statistics-body-trend-card__change');
        expect(root.querySelector('fd-ui-line-chart')).not.toBeNull();
        expect(changeLabel?.textContent.trim()).toBe(`${displayed} GENERAL.UNITS.KG`);
        expect(changeLabel?.classList.contains('statistics-body-trend-card__change--negative')).toBe(hasIncreaseClass);
        expect(changeLabel?.classList.contains('statistics-body-trend-card__change--positive')).toBe(false);
    });
});

function buttonByLabel(root: HTMLElement, label: string): HTMLButtonElement {
    const button = Array.from(root.querySelectorAll<HTMLButtonElement>('button')).find(candidate =>
        candidate.textContent.trim().endsWith(label),
    );
    if (button === undefined) {
        throw new Error(`Missing statistics metric control ${label}`);
    }
    return button;
}
