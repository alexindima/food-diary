import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { TranslateModule } from '@ngx-translate/core';
import { describe, expect, it, vi } from 'vitest';

import type { TdeeInsight } from '../../../models/tdee-insight.data';
import { TdeeInsightCardContentComponent } from './tdee-insight-card-content.component';

const EFFECTIVE_TDEE = 2100;
const SUGGESTED_TARGET = 1900;
const CURRENT_TARGET = 2000;

describe('TdeeInsightCardContentComponent', () => {
    it('derives confidence, hint, trend and suggestion state from insight', async () => {
        const { component, fixture } = await setupComponentAsync(createInsight());

        fixture.detectChanges();

        expect(component.confidenceLabel()).toBe('TDEE_CARD.CONFIDENCE.MEDIUM');
        expect(component.confidenceClass()).toBe('medium');
        expect(component.weightTrendFormatted()).toBe('-0.25');
        expect(component.hintKey()).toBe('TDEE_CARD.HINTS.REDUCE');
        expect(component.showSuggestion()).toBe(true);
    });

    it('emits apply goal click event', async () => {
        const { component } = await setupComponentAsync(createInsight());
        const event = new MouseEvent('click');
        const applySpy = vi.fn();
        component.applyGoal.subscribe(applySpy);

        component.applyGoal.emit(event);

        expect(applySpy).toHaveBeenCalledWith(event);
    });
});

async function setupComponentAsync(insight: TdeeInsight | null): Promise<{
    component: TdeeInsightCardContentComponent;
    fixture: ComponentFixture<TdeeInsightCardContentComponent>;
}> {
    await TestBed.resetTestingModule()
        .configureTestingModule({
            imports: [TdeeInsightCardContentComponent, TranslateModule.forRoot()],
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
