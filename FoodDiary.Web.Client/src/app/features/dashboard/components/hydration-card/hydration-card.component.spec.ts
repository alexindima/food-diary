import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { TranslateModule } from '@ngx-translate/core';
import { describe, expect, it, vi } from 'vitest';

import { HydrationCardComponent } from './hydration-card.component';
import { HYDRATION_CARD_ADD_STEP_ML } from './hydration-card.config';

const TOTAL_ML = 1500;
const GOAL_ML = 2000;
const EXPECTED_PERCENT = 75;
const OVER_GOAL_TOTAL_ML = 3000;
const MAX_TRACK_WIDTH = '130%';

describe('HydrationCardComponent', () => {
    it('calculates progress and caps track width for high values', async () => {
        const { component, fixture } = await setupComponentAsync({ total: TOTAL_ML, goal: GOAL_ML });

        fixture.detectChanges();

        expect(component.hasGoal()).toBe(true);
        expect(component.percent()).toBe(EXPECTED_PERCENT);

        fixture.componentRef.setInput('total', OVER_GOAL_TOTAL_ML);
        fixture.detectChanges();

        expect(component.trackWidth()).toBe(MAX_TRACK_WIDTH);
    });

    it('emits add amount only when adding is allowed', async () => {
        const { component, fixture } = await setupComponentAsync({ canAdd: false });
        const addSpy = vi.fn();
        component.addClick.subscribe(addSpy);

        component.onAdd();
        expect(addSpy).not.toHaveBeenCalled();

        fixture.componentRef.setInput('canAdd', true);
        fixture.detectChanges();
        component.onAdd();

        expect(addSpy).toHaveBeenCalledWith(HYDRATION_CARD_ADD_STEP_ML);
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
            imports: [HydrationCardComponent, TranslateModule.forRoot()],
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
