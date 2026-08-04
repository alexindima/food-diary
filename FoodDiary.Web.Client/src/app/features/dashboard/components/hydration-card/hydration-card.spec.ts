import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { describe, expect, it, vi } from 'vitest';

import { provideTranslateTesting } from '../../../../../testing/translate-testing.module';
import { HydrationCardComponent } from './hydration-card';
import { HYDRATION_CARD_ADD_AMOUNTS_ML, HYDRATION_CARD_PRIMARY_ADD_AMOUNT_ML } from './hydration-card.config';

const TOTAL_ML = 1500;
const GOAL_ML = 2000;
const EXPECTED_PERCENT = 75;
const OVER_GOAL_TOTAL_ML = 3000;
const FULL_LEVEL = '100%';

describe('HydrationCardComponent', () => {
    it('calculates progress, remaining amount, and caps the vessel fill level', async () => {
        const { component, fixture } = await setupComponentAsync({ total: TOTAL_ML, goal: GOAL_ML });

        fixture.detectChanges();

        expect(component['hasGoal']()).toBe(true);
        expect(component['percent']()).toBe(EXPECTED_PERCENT);
        expect(component['remaining']()).toBe(GOAL_ML - TOTAL_ML);

        fixture.componentRef.setInput('total', OVER_GOAL_TOTAL_ML);
        fixture.detectChanges();

        expect(component['fillLevel']()).toBe(FULL_LEVEL);
        expect(component['isGoalReached']()).toBe(true);
    });

    it('renders all quick amounts and emits the selected valid amount', async () => {
        const { component, fixture } = await setupComponentAsync();
        const addSpy = vi.fn();
        component['addClick'].subscribe(addSpy);
        fixture.detectChanges();

        const element = fixture.nativeElement as HTMLElement;
        const buttons = element.querySelectorAll('.hydration-card__quick-action');

        expect(buttons).toHaveLength(HYDRATION_CARD_ADD_AMOUNTS_ML.length);
        component['onAdd'](HYDRATION_CARD_PRIMARY_ADD_AMOUNT_ML);
        expect(addSpy).toHaveBeenCalledWith(HYDRATION_CARD_PRIMARY_ADD_AMOUNT_ML);
    });

    it('does not emit when adding is unavailable or the amount is unsupported', async () => {
        const { component, fixture } = await setupComponentAsync({ canAdd: false });
        const addSpy = vi.fn();
        component['addClick'].subscribe(addSpy);

        component['onAdd'](HYDRATION_CARD_PRIMARY_ADD_AMOUNT_ML);
        expect(addSpy).not.toHaveBeenCalled();

        fixture.componentRef.setInput('canAdd', true);
        fixture.detectChanges();
        component['onAdd'](1);

        expect(addSpy).not.toHaveBeenCalled();
    });
});

async function setupComponentAsync(
    overrides: Partial<{
        total: number;
        goal: number | null;
        isLoading: boolean;
        canAdd: boolean;
    }> = {},
): Promise<{
    component: HydrationCardComponent;
    fixture: ComponentFixture<HydrationCardComponent>;
}> {
    await TestBed.resetTestingModule()
        .configureTestingModule({
            imports: [HydrationCardComponent],
            providers: [provideTranslateTesting()],
        })
        .compileComponents();

    const fixture = TestBed.createComponent(HydrationCardComponent);
    fixture.componentRef.setInput('total', overrides.total ?? TOTAL_ML);
    fixture.componentRef.setInput('goal', overrides.goal ?? GOAL_ML);
    fixture.componentRef.setInput('isLoading', overrides.isLoading ?? false);
    fixture.componentRef.setInput('canAdd', overrides.canAdd ?? true);

    return {
        component: fixture.componentInstance,
        fixture,
    };
}
