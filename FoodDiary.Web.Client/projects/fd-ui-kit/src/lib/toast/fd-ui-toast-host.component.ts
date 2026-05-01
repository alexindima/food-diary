import { ChangeDetectionStrategy, Component, computed, inject } from '@angular/core';

import { FdUiToastHorizontalPosition, FdUiToastInstance, FdUiToastService, FdUiToastVerticalPosition } from './fd-ui-toast.service';

interface FdUiToastViewport {
    key: string;
    horizontalPosition: FdUiToastHorizontalPosition;
    verticalPosition: FdUiToastVerticalPosition;
    toasts: FdUiToastInstance[];
}

@Component({
    selector: 'fd-ui-toast-host',
    templateUrl: './fd-ui-toast-host.component.html',
    styleUrl: './fd-ui-toast-host.component.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class FdUiToastHostComponent {
    private readonly toastService = inject(FdUiToastService);

    protected readonly viewports = computed<FdUiToastViewport[]>(() => {
        const grouped = new Map<string, FdUiToastViewport>();

        for (const toast of this.toastService.toasts()) {
            const key = `${toast.verticalPosition}:${toast.horizontalPosition}`;
            const existing = grouped.get(key);

            if (existing) {
                existing.toasts.push(toast);
                continue;
            }

            grouped.set(key, {
                key,
                horizontalPosition: toast.horizontalPosition,
                verticalPosition: toast.verticalPosition,
                toasts: [toast],
            });
        }

        return Array.from(grouped.values());
    });

    protected trackViewport(_index: number, viewport: FdUiToastViewport): string {
        return viewport.key;
    }

    protected trackToast(_index: number, toast: FdUiToastInstance): string {
        return toast.id;
    }

    protected actionRole(toast: FdUiToastInstance): 'alert' | 'status' {
        return toast.politeness === 'assertive' || toast.appearance === 'negative' ? 'alert' : 'status';
    }

    protected dismiss(id: string): void {
        this.toastService.dismiss(id);
    }

    protected triggerAction(id: string): void {
        this.toastService.triggerAction(id);
    }
}
