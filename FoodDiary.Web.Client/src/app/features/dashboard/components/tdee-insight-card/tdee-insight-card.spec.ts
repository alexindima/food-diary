import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { describe, expect, it, vi } from 'vitest';

import { provideTranslateTesting } from '../../../../../testing/translate-testing.module';
import type { TdeeInsight } from '../../models/tdee-insight.data';
import { TdeeInsightCardComponent } from './tdee-insight-card';

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

        expect(component['effectiveTdee']()).toBe(ADAPTIVE_TDEE);
    });

    it('applies the suggested target through the actual button without opening the parent card', async () => {
        const { component, fixture } = await setupComponentAsync(createInsight());
        const applySpy = vi.fn();
        const parentClick = vi.fn();
        component.applyGoal.subscribe(applySpy);
        fixture.detectChanges();
        const host = fixture.nativeElement as HTMLElement;
        host.addEventListener('click', parentClick);
        const button = host.querySelector<HTMLButtonElement>('.tdee-card__calculated-goal button');
        expect(button).not.toBeNull();
        button?.click();
        expect(applySpy).toHaveBeenCalledExactlyOnceWith(SUGGESTED_TARGET);
        expect(parentClick).not.toHaveBeenCalled();
    });

    it.each([null, { ...createInsight(), suggestedCalorieTarget: null }])(
        'does not offer an apply action without a target',
        async insight => {
            const { fixture } = await setupComponentAsync(insight);
            fixture.detectChanges();
            expect((fixture.nativeElement as HTMLElement).querySelector('.tdee-card__calculated-goal')).toBeNull();
        },
    );

    it('shows loading instead of stale actionable data', async () => {
        const { fixture } = await setupComponentAsync(createInsight());
        fixture.componentRef.setInput('isLoading', true);
        fixture.detectChanges();
        const host = fixture.nativeElement as HTMLElement;
        expect(host.querySelector('.tdee-card__loading')).not.toBeNull();
        expect(host.querySelector('fd-tdee-insight-card-content')).toBeNull();
    });

    it('only explains the current estimate when viewing a historical date', async () => {
        const { fixture } = await setupComponentAsync(createInsight());
        fixture.detectChanges();
        const host = fixture.nativeElement as HTMLElement;
        expect(host.textContent).not.toContain('TDEE_CARD.CURRENT_ESTIMATE_NOTICE');
        fixture.componentRef.setInput('isHistorical', true);
        fixture.detectChanges();
        expect(host.textContent).toContain('TDEE_CARD.CURRENT_ESTIMATE_NOTICE');
        fixture.componentRef.setInput('isHistorical', false);
        fixture.detectChanges();
        expect(host.textContent).not.toContain('TDEE_CARD.CURRENT_ESTIMATE_NOTICE');
    });

    it('does not emit when suggested target is missing', async () => {
        const { component } = await setupComponentAsync({ ...createInsight(), suggestedCalorieTarget: null });
        const applySpy = vi.fn();
        component['applyGoal'].subscribe(applySpy);

        component['onApplyGoal']();

        expect(applySpy).not.toHaveBeenCalled();
    });
});

async function setupComponentAsync(insight: TdeeInsight | null): Promise<{
    component: TdeeInsightCardComponent;
    fixture: ComponentFixture<TdeeInsightCardComponent>;
}> {
    await TestBed.resetTestingModule()
        .configureTestingModule({
            imports: [TdeeInsightCardComponent],
            providers: [provideTranslateTesting()],
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
