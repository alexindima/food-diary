import { TestBed } from '@angular/core/testing';
import { afterEach, describe, expect, it } from 'vitest';

import { FdUiToastService } from './fd-ui-toast.service';
import { FdUiToastHostComponent } from './fd-ui-toast-host';

const GOAL_KCAL = 2000;
const UPDATED_KCAL = 800;

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
