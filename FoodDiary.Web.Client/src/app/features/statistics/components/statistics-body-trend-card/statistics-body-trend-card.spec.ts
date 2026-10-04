import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { TranslateService } from '@ngx-translate/core';
import { describe, expect, it } from 'vitest';

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
                points: [
                    { label: '20 Jul', value: 116 },
                    { label: '4 Aug', value: 113 },
                ],
            },
            waist: {
                key: 'waist',
                current: 99,
                change: -2,
                goal: 80,
                timeframeDays: 30,
                points: [
                    { label: '20 Jul', value: 101 },
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

        const translateService = TestBed.inject(TranslateService);
        translateService.use('ru');
        fixture.detectChanges();
        expect(root.querySelector('.fd-ui-line-chart__y-axis')?.textContent).toMatch(/\d,\d/);
        expect(root.querySelector('.fd-ui-line-chart__y-axis')?.textContent).not.toMatch(/\d\.\d/);
        translateService.use('en');
        fixture.detectChanges();
        expect(root.querySelector('.fd-ui-line-chart__y-axis')?.textContent).toMatch(/\d\.\d/);

        const toggle = root.querySelector('fd-ui-segmented-toggle');
        toggle?.dispatchEvent(new CustomEvent('selectedValueChange', { detail: 'waist' }));
    });
});
