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
    it('disables all additions while saving', async () => {
        const { component, fixture } = await setupComponentAsync({ isLoading: true });
        const add = vi.fn();
        component.addClick.subscribe(add);
        fixture.detectChanges();
        const buttons = (fixture.nativeElement as HTMLElement).querySelectorAll<HTMLButtonElement>('.hydration-card__quick-action button');
        expect(buttons.length).toBe(HYDRATION_CARD_ADD_AMOUNTS_ML.length);
        buttons.forEach(button => {
            expect(button.disabled).toBe(true);
            button.click();
        });
        expect(add).not.toHaveBeenCalled();
    });

    it('removes quick actions on historical days while retaining the recorded amount', async () => {
        const { fixture } = await setupComponentAsync({ canAdd: false });
        fixture.detectChanges();
        const host = fixture.nativeElement as HTMLElement;
        expect(host.querySelector('.hydration-card__quick-add')).toBeNull();
        expect(host.querySelector('[role="progressbar"]')?.getAttribute('aria-valuenow')).toBe(String(TOTAL_ML));
    });

    it.each([null, 0])('offers goal setup instead of an invalid progress bar for goal %s', async goal => {
        const { component, fixture } = await setupComponentAsync();
        fixture.componentRef.setInput('goal', goal);
        fixture.detectChanges();
        const action = vi.fn();
        component.goalAction.subscribe(action);
        const host = fixture.nativeElement as HTMLElement;
        expect(host.querySelector('[role="progressbar"]')).toBeNull();
        host.querySelector<HTMLButtonElement>('fd-notice-banner button')?.click();
        expect(action).toHaveBeenCalledOnce();
    });

    it('keeps accessible progress within its maximum after exceeding the goal', async () => {
        const { fixture } = await setupComponentAsync({ total: OVER_GOAL_TOTAL_ML });
        fixture.detectChanges();
        const host = fixture.nativeElement as HTMLElement;
        expect(host.querySelector('[role="progressbar"]')?.getAttribute('aria-valuenow')).toBe(String(GOAL_ML));
        expect(host.textContent).toContain('HYDRATION_CARD.GOAL_REACHED');
    });
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
        buttons.forEach(button => button.querySelector<HTMLButtonElement>('button')?.click());
        HYDRATION_CARD_ADD_AMOUNTS_ML.forEach((amount, index) => {
            expect(addSpy).toHaveBeenNthCalledWith(index + 1, amount);
        });
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
