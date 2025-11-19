import { Injectable, inject } from '@angular/core';
import { ComponentType } from '@angular/cdk/portal';
import { MatDialog, MatDialogConfig, MatDialogRef } from '@angular/material/dialog';
import { FdUiDialogSize } from './fd-ui-dialog.component';

export interface FdUiDialogConfig<D = unknown> extends MatDialogConfig<D> {
    size?: FdUiDialogSize;
}

@Injectable({
    providedIn: 'root',
})
export class FdUiDialogService {
    private readonly matDialog = inject(MatDialog);


    public open<T, D = unknown, R = unknown>(
        component: ComponentType<T>,
        config: FdUiDialogConfig<D> = {},
    ): MatDialogRef<T, R> {
        const size = config.size ?? 'md';
        const panelClass = this.mergeClasses(config.panelClass, ['fd-ui-dialog-panel', `fd-ui-dialog-panel--${size}`]);
        const backdropClass = this.mergeClasses(config.backdropClass, ['fd-ui-dialog-backdrop']);

        return this.matDialog.open(component, {
            ...config,
            panelClass,
            backdropClass,
            autoFocus: config.autoFocus ?? false,
        });
    }

    private mergeClasses(
        provided: string | string[] | undefined,
        base: string[],
    ): string[] {
        if (!provided) {
            return base;
        }

        const providedArray = Array.isArray(provided) ? provided : [provided];
        return [...base, ...providedArray];
    }
}
