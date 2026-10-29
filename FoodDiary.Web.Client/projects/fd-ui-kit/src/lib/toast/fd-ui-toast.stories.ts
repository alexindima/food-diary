import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import type { Meta, StoryObj } from '@storybook/angular-vite';

import { FdUiButtonComponent } from '../button/fd-ui-button';
import { FdUiToastService } from './fd-ui-toast.service';
import { FdUiToastHostComponent } from './fd-ui-toast-host';

@Component({
    selector: 'fd-toast-progress-example',
    imports: [FdUiButtonComponent, FdUiToastHostComponent],
    template: '<fd-ui-button (click)="showProgress()">Show daily progress</fd-ui-button><fd-ui-toast-host />',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
class ToastProgressExample {
    private readonly toast = inject(FdUiToastService);

    protected showProgress(): void {
        this.toast.success('Today: 850 / 2000 kcal', {
            progress: { value: 850, max: 2000, label: 'Daily calorie goal' },
        });
    }
}

export default { title: 'Feedback/Toast', component: ToastProgressExample } satisfies Meta<ToastProgressExample>;
export const DailyProgress: StoryObj<ToastProgressExample> = {};
