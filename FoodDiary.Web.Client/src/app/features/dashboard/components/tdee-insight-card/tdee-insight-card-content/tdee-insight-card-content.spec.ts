import { registerLocaleData } from '@angular/common';
import ru from '@angular/common/locales/ru';
import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { TranslateService } from '@ngx-translate/core';
import { describe, expect, it, vi } from 'vitest';

import { provideTranslateTesting } from '../../../../../../testing/translate-testing.module';
import { MeasurementSystemService } from '../../../../../shared/measurements/measurement-system.service';
import type { TdeeInsight } from '../../../../../shared/models/tdee-insight.data';
import { TdeeInsightCardContentComponent } from './tdee-insight-card-content';

registerLocaleData(ru);

const EFFECTIVE_TDEE = 2100;
const SUGGESTED_TARGET = 1900;
const CURRENT_TARGET = 2000;
const TREND_NEAR_CONVERSION_BOUNDARY_KG = 0.114;

describe('TdeeInsightCardContentComponent', () => {
    it('renders medium confidence and derives hint, trend and suggestion state', async () => {
        const { component, fixture } = await setupComponentAsync(createInsight());

        fixture.detectChanges();

        const host = fixture.nativeElement as HTMLElement;
        expect(host.querySelector('.tdee-card__confidence')?.textContent.trim()).toBe('TDEE_CARD.CONFIDENCE.MEDIUM');
        expect(host.textContent).toContain('TDEE_CARD.WEIGHT_TREND');
        expect(component['hintKey']()).toBe('TDEE_CARD.HINTS.REDUCE');
        expect(component['showSuggestion']()).toBe(true);
    });

    it('keeps the weekly trend numeric when switching measurement units and displays a measured zero', async () => {
        const insight = { ...createInsight(), weightTrendPerWeek: TREND_NEAR_CONVERSION_BOUNDARY_KG };
        const { fixture } = await setupComponentAsync(insight);
        TestBed.inject(TranslateService).use('en');
        const measurements = TestBed.inject(MeasurementSystemService);
        const previousSystem = measurements.system();
        const host = fixture.nativeElement as HTMLElement;
        const trendValue = (): string | undefined =>
            Array.from(host.querySelectorAll('.tdee-card__meta'))
                .find(row => row.textContent.includes('TDEE_CARD.WEIGHT_TREND'))
                ?.querySelector('.tdee-card__meta-value')
                ?.textContent.trim();
        try {
            measurements.setSystem('metric');
            fixture.detectChanges();
            expect(trendValue()).toBe('0.1 GENERAL.UNITS.KG/GENERAL.TIME.WEEK_SHORT');

            measurements.setSystem('imperial');
            fixture.detectChanges();
            expect(trendValue()).toBe('0.2 GENERAL.UNITS.LB/GENERAL.TIME.WEEK_SHORT');

            fixture.componentRef.setInput('insight', { ...insight, weightTrendPerWeek: 0 });
            fixture.detectChanges();
            expect(trendValue()).toBe('0.0 GENERAL.UNITS.LB/GENERAL.TIME.WEEK_SHORT');

            fixture.componentRef.setInput('insight', { ...insight, weightTrendPerWeek: null });
            fixture.detectChanges();
            expect(trendValue()).toBeUndefined();
        } finally {
            measurements.setSystem(previousSystem);
        }
    });

    it('updates grouping of the estimate and suggestion when changing language', async () => {
        const { fixture } = await setupComponentAsync(createInsight());
        const translate = TestBed.inject(TranslateService);
        translate.use('en');
        fixture.detectChanges();
        const host = fixture.nativeElement as HTMLElement;
        expect(host.querySelector('.tdee-card__number')?.textContent).toBe('2,100');
        translate.use('ru');
        fixture.detectChanges();
        expect(host.querySelector('.tdee-card__number')?.textContent.replaceAll(/\s/gu, ' ')).toBe('2 100');
        expect(host.querySelector('.tdee-card__calculated-goal')?.textContent.replaceAll(/\s/gu, ' ')).toContain('1 900');
    });

    it('emits apply goal click event', async () => {
        const { component, fixture } = await setupComponentAsync(createInsight());
        fixture.detectChanges();
        const event = new MouseEvent('click', { bubbles: true });
        const applySpy = vi.fn();
        component['applyGoal'].subscribe(applySpy);

        (fixture.nativeElement as HTMLElement).querySelector('button')?.dispatchEvent(event);

        expect(applySpy).toHaveBeenCalledWith(event);
    });
});

async function setupComponentAsync(insight: TdeeInsight | null): Promise<{
    component: TdeeInsightCardContentComponent;
    fixture: ComponentFixture<TdeeInsightCardContentComponent>;
}> {
    await TestBed.resetTestingModule()
        .configureTestingModule({
            imports: [TdeeInsightCardContentComponent],
            providers: [provideTranslateTesting()],
        })
        .compileComponents();

    const fixture = TestBed.createComponent(TdeeInsightCardContentComponent);
    fixture.componentRef.setInput('insight', insight);
    fixture.componentRef.setInput('effectiveTdee', EFFECTIVE_TDEE);

    return {
        component: fixture.componentInstance,
        fixture,
    };
}

function createInsight(): TdeeInsight {
    return {
        estimatedTdee: EFFECTIVE_TDEE,
        adaptiveTdee: null,
        bmr: 1600,
        suggestedCalorieTarget: SUGGESTED_TARGET,
        currentCalorieTarget: CURRENT_TARGET,
        weightTrendPerWeek: -0.25,
        confidence: 'medium',
        dataDaysUsed: 20,
        goalAdjustmentHint: 'hint.reduce',
    };
}
