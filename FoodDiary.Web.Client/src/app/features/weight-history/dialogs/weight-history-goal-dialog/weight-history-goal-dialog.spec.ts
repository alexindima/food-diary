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
