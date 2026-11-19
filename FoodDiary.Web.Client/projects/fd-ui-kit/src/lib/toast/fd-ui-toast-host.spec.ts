import { TestBed } from '@angular/core/testing';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';

import { FdUiToastService } from './fd-ui-toast.service';
import { FdUiToastHostComponent } from './fd-ui-toast-host';

const GOAL_KCAL = 2000;
const UPDATED_KCAL = 800;
const EXIT_ANIMATION_MS = 200;

describe('toast progress', () => {
    afterEach(() => {
        TestBed.inject(FdUiToastService).dismissAll();
    });

    it('renders accessible goal progress and preserves the actual total above the goal', () => {
        const fixture = TestBed.createComponent(FdUiToastHostComponent);
        const service = TestBed.inject(FdUiToastService);
        service.success('Today: 2300 / 2000 kcal', { duration: 0, progress: { value: 2300, max: 2000, label: 'Daily calorie goal' } });
        fixture.detectChanges();
        const host = fixture.nativeElement as HTMLElement;
        const progress = host.querySelector('progress');
        expect(progress?.getAttribute('aria-label')).toBe('Daily calorie goal');
        expect(progress?.value).toBe(GOAL_KCAL);
        expect(host.textContent).toContain('2300 / 2000');
    });

    it('updates progress when a matching notification is refreshed', () => {
        const fixture = TestBed.createComponent(FdUiToastHostComponent);
        const service = TestBed.inject(FdUiToastService);
        service.open('Daily progress', { duration: 0, progress: { value: 500, max: 2000, label: 'Goal' } });
        service.open('Daily progress', { duration: 0, progress: { value: 800, max: 2000, label: 'Goal' } });
        fixture.detectChanges();
        const host = fixture.nativeElement as HTMLElement;
        expect(host.querySelectorAll('progress')).toHaveLength(1);
        expect(host.querySelector('progress')?.value).toBe(UPDATED_KCAL);
    });

    it('merges save confirmation and progress in either response order', () => {
        const fixture = TestBed.createComponent(FdUiToastHostComponent);
        const service = TestBed.inject(FdUiToastService);
        const confirmation = service.success('Meal saved', { key: 'meal-feedback', duration: 0 });
        const progress = service.success('Today: 800 / 2000 kcal', {
            key: 'meal-feedback',
            duration: 0,
            progress: { value: UPDATED_KCAL, max: GOAL_KCAL, label: 'Goal' },
        });
        service.success('Meal saved', { key: 'meal-feedback', duration: 0 });
        fixture.detectChanges();

        const host = fixture.nativeElement as HTMLElement;
        expect(progress).toBe(confirmation);
        expect(service.toasts()).toHaveLength(1);
        expect(host.querySelector('progress')?.value).toBe(UPDATED_KCAL);
        expect(host.textContent).toContain('Today: 800 / 2000 kcal');
    });
});

describe('toast placement and actions', () => {
    let service: FdUiToastService;

    beforeEach(() => {
        vi.useFakeTimers();
        service = TestBed.inject(FdUiToastService);
    });

    afterEach(() => {
        service.dismissAll();
        vi.runOnlyPendingTimers();
        vi.useRealTimers();
    });

    it('groups messages in the same viewport and makes urgent or negative feedback an alert', () => {
        const fixture = TestBed.createComponent(FdUiToastHostComponent);
        service.info('Changes available', { duration: 0, horizontalPosition: 'end' });
        service.open('Cannot undo', { duration: 0, appearance: 'negative', politeness: 'polite', horizontalPosition: 'end' });
        service.info('Check connection', { duration: 0, politeness: 'assertive', verticalPosition: 'top', horizontalPosition: 'start' });
        fixture.detectChanges();
        const host = fixture.nativeElement as HTMLElement;
        const viewports = host.querySelectorAll('.fd-ui-toast-host__viewport');
        expect(viewports).toHaveLength(2);
        const bottom = host.querySelector('.fd-ui-toast-host__viewport--bottom.fd-ui-toast-host__viewport--end');
        expect(bottom?.querySelector('[role="status"]')?.textContent).toContain('Changes available');
        expect(bottom?.querySelector('[role="alert"]')?.textContent).toContain('Cannot undo');
        const top = host.querySelector('.fd-ui-toast-host__viewport--top.fd-ui-toast-host__viewport--start');
        expect(top?.querySelector('[role="alert"][aria-live="assertive"]')?.textContent).toContain('Check connection');
    });

    it('emits an action once, then dismisses after the exit animation without closing another toast', () => {
        const fixture = TestBed.createComponent(FdUiToastHostComponent);
        fixture.componentRef.setInput('closeLabel', 'Dismiss notice');
        const actionable = service.open('Meal removed', { action: 'Undo', duration: 0 });
        const other = service.open('Ready', { duration: 0 });
        const action = vi.fn();
        const dismissed = vi.fn();
        const otherDismissed = vi.fn();
        actionable.onAction().subscribe(action);
        actionable.afterDismissed().subscribe(dismissed);
        other.afterDismissed().subscribe(otherDismissed);
        fixture.detectChanges();
        const host = fixture.nativeElement as HTMLElement;
        const undo = host.querySelector<HTMLButtonElement>('.fd-ui-toast-host__action');
        undo?.click();
        fixture.detectChanges();

        expect(action).toHaveBeenCalledOnce();
        expect(dismissed).not.toHaveBeenCalled();
        expect(undo?.disabled).toBe(true);
        undo?.click();
        vi.advanceTimersByTime(EXIT_ANIMATION_MS);
        fixture.detectChanges();
        expect(action).toHaveBeenCalledOnce();
        expect(dismissed).toHaveBeenCalledExactlyOnceWith({ dismissedByAction: true });
        expect(host.textContent).not.toContain('Meal removed');
        expect(otherDismissed).not.toHaveBeenCalled();

        host.querySelector<HTMLButtonElement>('button[aria-label="Dismiss notice"]')?.click();
        vi.advanceTimersByTime(EXIT_ANIMATION_MS);
        fixture.detectChanges();
        expect(otherDismissed).toHaveBeenCalledExactlyOnceWith({ dismissedByAction: false });
        expect(host.querySelector('.fd-ui-toast-host__viewport')).toBeNull();
    });
});
