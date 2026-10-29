import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { FdUiDialogService } from 'fd-ui-kit/dialog/fd-ui-dialog.service';
import { type Observable, of, Subject } from 'rxjs';
import { afterEach, describe, expect, it, vi } from 'vitest';

import { provideTranslateTesting } from '../../../../../testing/translate-testing.module';
import { HydrationDetailsDialogComponent } from '../../dialogs/hydration-details-dialog/hydration-details-dialog';
import { HydrationCardComponent } from './hydration-card';
import { HYDRATION_CARD_ADD_AMOUNTS_ML, HYDRATION_CARD_FEEDBACK_MS } from './hydration-card.config';

const TOTAL_ML = 1500;
const GOAL_ML = 2000;
const EXPECTED_PERCENT = 75;
const OVER_GOAL_TOTAL_ML = 3000;
const FULL_LEVEL = '100%';
const VALID_ADD_AMOUNT_ML = 250;

describe('HydrationCardComponent', () => {
    it('offers undo for the recorded entry even after switching to a past day and locks it while updating', async () => {
        const { component, fixture } = await setupComponentAsync({ canAdd: false });
        fixture.componentRef.setInput('undoAmount', VALID_ADD_AMOUNT_ML);
        fixture.detectChanges();
        const undo = vi.fn();
        component.undoClick.subscribe(undo);
        const button = (fixture.nativeElement as HTMLElement).querySelector<HTMLButtonElement>('.hydration-card__undo button');
        button?.click();
        expect(undo).toHaveBeenCalledOnce();
        fixture.componentRef.setInput('isLoading', true);
        fixture.detectChanges();
        expect(button?.disabled).toBe(true);
        button?.click();
        expect(undo).toHaveBeenCalledOnce();
    });
});

describe('HydrationCardComponent saved feedback', () => {
    afterEach(() => vi.useRealTimers());

    it('confirms a saved entry, clears only the animation after a short delay and keeps undo available', async () => {
        const { fixture } = await setupComponentAsync();
        fixture.detectChanges();
        vi.useFakeTimers();
        const host = fixture.nativeElement as HTMLElement;
        host.querySelectorAll<HTMLButtonElement>('.hydration-card__quick-action button')[1].click();
        fixture.componentRef.setInput('isLoading', true);
        fixture.detectChanges();
        expect(host.querySelector('.hydration-card__added-volume')).toBeNull();

        confirmEntry(fixture, 'first');
        expect(host.querySelector('.hydration-card__added-volume')?.textContent).toContain('+250');
        expect(host.querySelector('.hydration-card__quick-action--confirmed')).not.toBeNull();
        expect(host.querySelector('[role="status"]')?.textContent).toContain('HYDRATION_CARD.ADDED_AMOUNT');

        vi.advanceTimersByTime(HYDRATION_CARD_FEEDBACK_MS);
        fixture.detectChanges();
        expect(host.querySelector('.hydration-card__added-volume')).toBeNull();
        expect(host.querySelector('.hydration-card__quick-action--confirmed')).toBeNull();
        expect(host.querySelector<HTMLButtonElement>('.hydration-card__undo button')?.disabled).toBe(false);
    });

    it('animates consecutive equal portions independently and never treats undo as another addition', async () => {
        const { component, fixture } = await setupComponentAsync();
        fixture.detectChanges();
        const host = fixture.nativeElement as HTMLElement;
        for (const id of ['first', 'second']) {
            component['onAdd'](VALID_ADD_AMOUNT_ML);
            fixture.componentRef.setInput('isLoading', true);
            fixture.detectChanges();
            expect(host.querySelector('.hydration-card__added-volume')).toBeNull();
            confirmEntry(fixture, id);
            expect(host.querySelector('.hydration-card__added-volume')).not.toBeNull();
        }
        host.querySelector<HTMLButtonElement>('.hydration-card__undo button')?.click();
        fixture.componentRef.setInput('undoEntryId', 'first');
        fixture.detectChanges();
        expect(host.querySelector('.hydration-card__added-volume')).toBeNull();
    });

    it('keeps the preceding undo on failure without showing a successful animation', async () => {
        const { component, fixture } = await setupComponentAsync();
        confirmEntry(fixture, 'previous');
        component['onAdd'](VALID_ADD_AMOUNT_ML);
        fixture.componentRef.setInput('isLoading', true);
        fixture.detectChanges();
        fixture.componentRef.setInput('isLoading', false);
        fixture.detectChanges();
        const host = fixture.nativeElement as HTMLElement;
        expect(host.querySelector('.hydration-card__added-volume')).toBeNull();
        expect(host.querySelector<HTMLButtonElement>('.hydration-card__undo button')?.disabled).toBe(false);
    });
});

describe('HydrationCardComponent context changes and details', () => {
    it('does not flash an addition on a historical day selected while the request was saving', async () => {
        const { component, fixture } = await setupComponentAsync();
        fixture.detectChanges();
        component['onAdd'](VALID_ADD_AMOUNT_ML);
        fixture.componentRef.setInput('isLoading', true);
        fixture.detectChanges();
        fixture.componentRef.setInput('canAdd', false);
        confirmEntry(fixture, 'historical-entry');
        expect((fixture.nativeElement as HTMLElement).querySelector('.hydration-card__added-volume')).toBeNull();
    });

    it('does not mistake undo for a successful addition after an immediate failure', async () => {
        const { component, fixture } = await setupComponentAsync();
        confirmEntry(fixture, 'previous');
        component['onAdd'](VALID_ADD_AMOUNT_ML);
        component['onUndo']();
        confirmEntry(fixture, 'older');
        expect((fixture.nativeElement as HTMLElement).querySelector('.hydration-card__added-volume')).toBeNull();
    });

    it('opens details through the glass button and forwards the goal action only when requested', async () => {
        const closed = new Subject<boolean | undefined>();
        const { component, fixture, dialogOpen } = await setupComponentAsync({}, closed);
        const goal = vi.fn();
        component.goalAction.subscribe(goal);
        fixture.detectChanges();
        const button = (fixture.nativeElement as HTMLElement).querySelector<HTMLButtonElement>('.hydration-card__details button');
        expect(button?.getAttribute('aria-haspopup')).toBe('dialog');
        expect(button?.getAttribute('aria-label')).toBe('HYDRATION_CARD.OPEN_DETAILS');
        button?.click();
        expect(dialogOpen).toHaveBeenCalledWith(HydrationDetailsDialogComponent, {
            size: 'sm',
            data: { total: TOTAL_ML, history: null },
        });
        closed.next(undefined);
        expect(goal).not.toHaveBeenCalled();
        closed.next(true);
        expect(goal).toHaveBeenCalledOnce();
    });
});

function confirmEntry(fixture: ComponentFixture<HydrationCardComponent>, id: string): void {
    fixture.componentRef.setInput('undoEntryId', id);
    fixture.componentRef.setInput('undoAmount', VALID_ADD_AMOUNT_ML);
    fixture.componentRef.setInput('isLoading', false);
    fixture.detectChanges();
}

describe('HydrationCardComponent compact layout', () => {
    it('does not reserve an empty confirmation row before adding water', async () => {
        const { fixture } = await setupComponentAsync();
        fixture.componentRef.setInput('undoAmount', null);
        fixture.detectChanges();
        expect((fixture.nativeElement as HTMLElement).querySelector('.hydration-card__undo-row')).toBeNull();
    });
});

describe('HydrationCardComponent amount and progress', () => {
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

        component['onAdd'](VALID_ADD_AMOUNT_ML);
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
    closed: Observable<boolean | undefined> = of(undefined),
): Promise<{
    component: HydrationCardComponent;
    fixture: ComponentFixture<HydrationCardComponent>;
    dialogOpen: ReturnType<typeof vi.fn>;
}> {
    const dialogOpen = vi.fn(() => ({ afterClosed: (): Observable<boolean | undefined> => closed }));
    await TestBed.resetTestingModule()
        .configureTestingModule({
            imports: [HydrationCardComponent],
            providers: [
                provideTranslateTesting(),
                {
                    provide: FdUiDialogService,
                    useValue: { open: dialogOpen },
                },
            ],
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
        dialogOpen,
    };
}
