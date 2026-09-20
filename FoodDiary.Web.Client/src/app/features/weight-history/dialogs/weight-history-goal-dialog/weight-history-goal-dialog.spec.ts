import { signal, type WritableSignal } from '@angular/core';
import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { type FieldTree, form } from '@angular/forms/signals';
import { FdUiDialogRef } from 'fd-ui-kit/dialog/fd-ui-dialog-ref';
import { describe, expect, it, type Mock, vi } from 'vitest';

import { provideTranslateTesting } from '../../../../../testing/translate-testing.module';
import { WeightHistoryFacade } from '../../lib/weight-history.facade';
import { WeightHistoryGoalDialogComponent } from './weight-history-goal-dialog';

const FIXTURE_INITIAL_VERSION = 7;
const FIXTURE_TARGET_MEASUREMENT = 75;
const createFacade = (): {
    form: FieldTree<{ date: string; weight: string }>;
    desiredWeightForm: FieldTree<{ date: string; weight: string }>;
    isSaving: WritableSignal<boolean>;
    isDesiredWeightSaving: WritableSignal<boolean>;
    isEditing: WritableSignal<boolean>;
    entryError: WritableSignal<string | null>;
    entrySaveVersion: WritableSignal<number>;
    desiredWeightSaveVersion: WritableSignal<number>;
    desiredWeightKg: WritableSignal<number | null>;
    cancelEdit: Mock;
    saveDesiredWeight: Mock;
    cancelWeightGoal: Mock;
} => {
    const model = signal({ date: '2026-04-01', weight: '80' });
    const fields = form(model);
    return {
        form: fields,
        desiredWeightForm: fields,
        isSaving: signal(false),
        isDesiredWeightSaving: signal(false),
        isEditing: signal(false),
        entryError: signal<string | null>(null),
        entrySaveVersion: signal(FIXTURE_INITIAL_VERSION),
        desiredWeightSaveVersion: signal(FIXTURE_INITIAL_VERSION),
        desiredWeightKg: signal<number | null>(FIXTURE_TARGET_MEASUREMENT),
        cancelEdit: vi.fn(),
        saveDesiredWeight: vi.fn(),
        cancelWeightGoal: vi.fn(),
    } as const;
};

function setup(): {
    fixture: ComponentFixture<WeightHistoryGoalDialogComponent>;
    component: WeightHistoryGoalDialogComponent;
    facade: ReturnType<typeof createFacade>;
    close: Mock;
} {
    const close = vi.fn();
    TestBed.configureTestingModule({
        imports: [WeightHistoryGoalDialogComponent],
        providers: [
            provideTranslateTesting(),
            { provide: WeightHistoryFacade, useFactory: createFacade },
            { provide: FdUiDialogRef, useValue: { close } },
        ],
    });
    const facade = TestBed.inject(WeightHistoryFacade) as unknown as ReturnType<typeof createFacade>;
    const fixture = TestBed.createComponent(WeightHistoryGoalDialogComponent);
    fixture.detectChanges();
    return { fixture, facade, component: fixture.componentInstance, close };
}
describe('Weight goal dialog lifecycle', () => {
    it('does not close for an old save or loading-state changes', () => {
        const { fixture, facade, close } = setup();
        facade.isDesiredWeightSaving.set(true);
        fixture.detectChanges();
        facade.isDesiredWeightSaving.set(false);
        fixture.detectChanges();
        expect(close).not.toHaveBeenCalled();
    });
    it('closes only when a new successful save arrives', () => {
        const { fixture, facade, close } = setup();
        facade.desiredWeightSaveVersion.update(v => v + 1);
        fixture.detectChanges();
        expect(close).toHaveBeenCalledOnce();
    });
    it('unsubscribes the save effect when destroyed', () => {
        const { fixture, facade, close } = setup();
        fixture.destroy();
        facade.desiredWeightSaveVersion.update(v => v + 1);
        TestBed.tick();
        expect(close).not.toHaveBeenCalled();
    });
    it('delegates save and cancellation through real buttons', () => {
        const { fixture, facade } = setup();
        const buttons = Array.from((fixture.nativeElement as HTMLElement).querySelectorAll('button'));
        buttons.find(b => b.textContent.includes('WEIGHT_HISTORY.START_NEW_GOAL'))?.click();
        expect(facade.saveDesiredWeight).toHaveBeenCalledOnce();
        buttons.find(b => b.textContent.includes('WEIGHT_HISTORY.CANCEL_GOAL'))?.click();
        expect(facade.cancelWeightGoal).toHaveBeenCalledOnce();
    });
    it('offers creation with no active goal and closes explicitly', () => {
        const { fixture, facade, component, close } = setup();
        facade.desiredWeightKg.set(null);
        fixture.detectChanges();
        const text = (fixture.nativeElement as HTMLElement).textContent;
        expect(text).toContain('WEIGHT_HISTORY.SET_GOAL');
        expect(text).not.toContain('WEIGHT_HISTORY.CANCEL_GOAL');
        component['close']();
        expect(close).toHaveBeenCalledOnce();
    });
});

describe('Weight goal keyboard submission', () => {
    it('preserves comma input and submits without navigation', () => {
        const { fixture, facade } = setup();
        const root = fixture.nativeElement as HTMLElement;
        const value = root.querySelector<HTMLInputElement>('fd-ui-input input');
        expect(value?.type).toBe('text');
        expect(value?.inputMode).toBe('decimal');
        if (value === null) {
            throw new Error('Missing goal input');
        }
        value.value = '72,5';
        value.dispatchEvent(new Event('input', { bubbles: true }));
        fixture.detectChanges();
        const event = new Event('submit', { bubbles: true, cancelable: true });
        root.querySelector('form')?.dispatchEvent(event);
        expect(event.defaultPrevented).toBe(true);
        expect(facade.desiredWeightForm.weight().value()).toBe('72,5');
        expect(facade.saveDesiredWeight).toHaveBeenCalledOnce();
    });
    it('does not submit an empty target or resubmit while saving', () => {
        const { component, facade } = setup();
        facade.desiredWeightForm.weight().value.set('');
        component['save']();
        expect(facade.saveDesiredWeight).not.toHaveBeenCalled();
        expect(facade.cancelWeightGoal).not.toHaveBeenCalled();
        facade.desiredWeightForm.weight().value.set('72');
        facade.isDesiredWeightSaving.set(true);
        component['save']();
        component['cancelGoal']();
        expect(facade.saveDesiredWeight).not.toHaveBeenCalled();
        expect(facade.cancelWeightGoal).not.toHaveBeenCalled();
    });
});

describe('Weight goal rendered controls', () => {
    it.each(['', '   '])('disables submission for blank target %j without cancelling the goal', value => {
        const { fixture, facade } = setup();
        facade.desiredWeightForm.weight().value.set(value);
        fixture.detectChanges();
        const root = fixture.nativeElement as HTMLElement;
        const submitButton = root.querySelector<HTMLButtonElement>('button[type="submit"]');
        expect(submitButton?.disabled).toBe(true);
        root.querySelector('form')?.dispatchEvent(new Event('submit', { bubbles: true, cancelable: true }));
        expect(facade.saveDesiredWeight).not.toHaveBeenCalled();
        expect(facade.cancelWeightGoal).not.toHaveBeenCalled();
        expect(findGoalButton(root, 'WEIGHT_HISTORY.CANCEL_GOAL').disabled).toBe(false);
    });

    it('disables saving, cancellation and dismissal while a request is pending', () => {
        const { fixture, facade, close } = setup();
        expect((fixture.nativeElement as HTMLElement).querySelector('.fd-ui-dialog__close-button')).not.toBeNull();
        facade.isDesiredWeightSaving.set(true);
        fixture.detectChanges();
        const root = fixture.nativeElement as HTMLElement;
        const save = findGoalButton(root, 'WEIGHT_HISTORY.START_NEW_GOAL');
        const cancel = findGoalButton(root, 'WEIGHT_HISTORY.CANCEL_GOAL');
        expect(save.disabled).toBe(true);
        expect(cancel.disabled).toBe(true);
        expect(root.querySelector('.fd-ui-dialog__close-button')).toBeNull();
        save.click();
        cancel.click();
        expect(facade.saveDesiredWeight).not.toHaveBeenCalled();
        expect(facade.cancelWeightGoal).not.toHaveBeenCalled();
        expect(close).not.toHaveBeenCalled();
    });

    it('dismisses creation with Cancel without mutating a goal', () => {
        const { fixture, facade, close } = setup();
        facade.desiredWeightKg.set(null);
        fixture.detectChanges();
        const root = fixture.nativeElement as HTMLElement;
        findGoalButton(root, 'WEIGHT_HISTORY.CANCEL_EDIT').click();
        expect(close).toHaveBeenCalledOnce();
        expect(facade.saveDesiredWeight).not.toHaveBeenCalled();
        expect(facade.cancelWeightGoal).not.toHaveBeenCalled();
        expect(root.textContent).not.toContain('WEIGHT_HISTORY.GOAL_HISTORY_HINT');
    });

    it('preserves the draft and allows retry when a request ends without success', () => {
        const { fixture, facade, close } = setup();
        const root = fixture.nativeElement as HTMLElement;
        facade.desiredWeightForm.weight().value.set('72,5');
        fixture.detectChanges();
        findGoalButton(root, 'WEIGHT_HISTORY.START_NEW_GOAL').click();
        facade.isDesiredWeightSaving.set(true);
        fixture.detectChanges();
        facade.isDesiredWeightSaving.set(false);
        fixture.detectChanges();
        expect(close).not.toHaveBeenCalled();
        expect(root.querySelector<HTMLInputElement>('fd-ui-input input')?.value).toBe('72,5');
        const retry = findGoalButton(root, 'WEIGHT_HISTORY.START_NEW_GOAL');
        expect(retry.disabled).toBe(false);
        retry.click();
        expect(facade.saveDesiredWeight).toHaveBeenCalledTimes(2);
    });
});

function findGoalButton(root: HTMLElement, text: string): HTMLButtonElement {
    const button = Array.from(root.querySelectorAll('button')).find(item => item.textContent.includes(text));
    if (button === undefined) {
        throw new Error(`Missing goal button: ${text}`);
    }
    return button;
}
