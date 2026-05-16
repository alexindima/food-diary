import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { TranslateModule } from '@ngx-translate/core';
import { describe, expect, it, vi } from 'vitest';

import type { TdeeInsight } from '../../models/tdee-insight.data';
import { TdeeInsightCardComponent } from './tdee-insight-card.component';

const ESTIMATED_TDEE = 2300;
const ADAPTIVE_TDEE = 2450;
const SUGGESTED_TARGET = 2100;

describe('TdeeInsightCardComponent', () => {
    it('uses adaptive tdee as the effective value when available', async () => {
        const { component, fixture } = await setupComponentAsync({
            ...createInsight(),
            adaptiveTdee: ADAPTIVE_TDEE,
            estimatedTdee: ESTIMATED_TDEE,
        });

        fixture.detectChanges();

        expect(component.effectiveTdee()).toBe(ADAPTIVE_TDEE);
    });

    it('emits suggested target and stops click propagation', async () => {
        const { component } = await setupComponentAsync(createInsight());
        const applySpy = vi.fn();
        const stopPropagation = vi.fn();
        component.applyGoal.subscribe(applySpy);

        component.onApplyGoal({ stopPropagation } as unknown as Event);

        expect(stopPropagation).toHaveBeenCalledTimes(1);
        expect(applySpy).toHaveBeenCalledWith(SUGGESTED_TARGET);
    });

    it('does not emit when suggested target is missing', async () => {
        const { component } = await setupComponentAsync({ ...createInsight(), suggestedCalorieTarget: null });
        const applySpy = vi.fn();
        component.applyGoal.subscribe(applySpy);

        component.onApplyGoal();

        expect(applySpy).not.toHaveBeenCalled();
    });
});

async function setupComponentAsync(insight: TdeeInsight | null): Promise<{
    component: TdeeInsightCardComponent;
    fixture: ComponentFixture<TdeeInsightCardComponent>;
}> {
    await TestBed.resetTestingModule()
        .configureTestingModule({
            imports: [TdeeInsightCardComponent, TranslateModule.forRoot()],
        })
        .compileComponents();

    const fixture = TestBed.createComponent(TdeeInsightCardComponent);
    fixture.componentRef.setInput('insight', insight);
    fixture.componentRef.setInput('isLoading', false);

    return {
        component: fixture.componentInstance,
        fixture,
    };
}

function createInsight(): TdeeInsight {
    return {
        estimatedTdee: ESTIMATED_TDEE,
        adaptiveTdee: null,
        bmr: 1700,
        suggestedCalorieTarget: SUGGESTED_TARGET,
        currentCalorieTarget: 2000,
        weightTrendPerWeek: -0.25,
        confidence: 'medium',
        dataDaysUsed: 21,
        goalAdjustmentHint: 'increase',
    };
}
