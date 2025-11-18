import { Injectable, inject } from '@angular/core';
import {
    MatSnackBar,
    MatSnackBarConfig,
    MatSnackBarRef,
    SimpleSnackBar,
} from '@angular/material/snack-bar';

export type FdUiToastAppearance = 'default' | 'positive' | 'negative' | 'warning' | 'info';

export interface FdUiToastOptions extends MatSnackBarConfig {
    action?: string;
    appearance?: FdUiToastAppearance;
}

@Injectable({
    providedIn: 'root',
})
export class FdUiToastService {
    private readonly snackBar = inject(MatSnackBar);

    public open(message: string, options: FdUiToastOptions = {}): MatSnackBarRef<SimpleSnackBar> {
        const { action, appearance = 'default', ...config } = options;
        const panelClass = this.mergePanelClasses(config.panelClass, appearance);

        return this.snackBar.open(message, action, {
            duration: config.duration ?? 5000,
            horizontalPosition: config.horizontalPosition ?? 'center',
            verticalPosition: config.verticalPosition ?? 'top',
            panelClass,
            politeness: config.politeness ?? 'polite',
        });
    }

    private mergePanelClasses(
        provided: MatSnackBarConfig['panelClass'],
        appearance: FdUiToastAppearance,
    ): string[] {
        const appearanceClass = `fd-ui-toast--${appearance}`;
        const baseClasses = ['fd-ui-toast', appearanceClass];
        if (!provided) {
            return baseClasses;
        }

        return baseClasses.concat(provided instanceof Array ? provided : [provided]);
    }
}
