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
        const providedPanelClasses = this.asArray(config.panelClass);
        const isFullscreen = providedPanelClasses.includes('fd-ui-dialog-panel--fullscreen');
        const isEdgeMobile = this.isCompactMobile() && !isFullscreen;
        const mobileClasses = isEdgeMobile ? ['fd-ui-dialog-panel--edge-mobile'] : [];
        const panelClass = this.mergeClasses(config.panelClass, ['fd-ui-dialog-panel', `fd-ui-dialog-panel--${size}`, ...mobileClasses]);
        const backdropClass = this.mergeClasses(config.backdropClass, ['fd-ui-dialog-backdrop']);

        return this.matDialog.open(component, {
            ...config,
            width: config.width ?? (isEdgeMobile ? '100vw' : undefined),
            maxWidth: config.maxWidth ?? (isEdgeMobile ? '100vw' : undefined),
            position: config.position ?? (isEdgeMobile ? { bottom: '0' } : undefined),
            panelClass,
            backdropClass,
            autoFocus: config.autoFocus ?? false,
        });
    }

    private mergeClasses(
        provided: string | string[] | undefined,
        base: string[],
    ): string[] {
        const providedArray = this.asArray(provided);
        if (providedArray.length === 0) {
            return base;
        }

        return [...base, ...providedArray];
    }

    private asArray(classes: string | string[] | undefined): string[] {
        if (!classes) {
            return [];
        }
        return Array.isArray(classes) ? classes : [classes];
    }

    private isCompactMobile(): boolean {
        return typeof window !== 'undefined' && window.matchMedia('(max-width: 420px)').matches;
    }
}
