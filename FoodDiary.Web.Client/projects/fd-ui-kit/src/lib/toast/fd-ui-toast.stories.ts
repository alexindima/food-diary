import { ChangeDetectionStrategy, Component, inject, input } from '@angular/core';
import type { Meta, StoryObj } from '@storybook/angular-vite';

import { FdUiButtonComponent } from '../button/fd-ui-button';
import { FdUiToastService } from './fd-ui-toast.service';
import { FdUiToastHostComponent } from './fd-ui-toast-host';

@Component({
    selector: 'fd-toast-progress-example',
    imports: [FdUiButtonComponent, FdUiToastHostComponent],
    template: '<fd-ui-button (click)="showToast()">{{ undo() ? "Add 250 ml" : "Show daily progress" }}</fd-ui-button><fd-ui-toast-host />',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
class ToastProgressExample {
    private readonly toast = inject(FdUiToastService);
    public readonly undo = input(false);

    protected showToast(): void {
        if (this.undo()) {
            this.toast.success('Added 250 ml', { action: 'Undo', duration: 8000 });
            return;
        }
        this.toast.success('Today: 850 / 2000 kcal', {
            progress: { value: 850, max: 2000, label: 'Daily calorie goal' },
        });
    }
}

export default { title: 'Feedback/Toast', component: ToastProgressExample } satisfies Meta<ToastProgressExample>;
export const DailyProgress: StoryObj<ToastProgressExample> = {};
export const Undo: StoryObj<ToastProgressExample> = { args: { undo: true } };
