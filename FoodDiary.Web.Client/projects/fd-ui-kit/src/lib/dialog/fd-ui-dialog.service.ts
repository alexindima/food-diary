import { Injectable, inject } from '@angular/core';
import { ComponentType } from '@angular/cdk/portal';
import { Dialog, DialogConfig } from '@angular/cdk/dialog';
import { Overlay } from '@angular/cdk/overlay';
import { StaticProvider } from '@angular/core';
import { FdUiDialogRef } from './fd-ui-dialog-ref';
import { FdUiDialogSize } from './fd-ui-dialog.component';

export interface FdUiDialogConfig<D = unknown> extends Omit<DialogConfig<D>, 'providers' | 'container'> {
    size?: FdUiDialogSize;
    providers?: StaticProvider[];
}

@Injectable({
    providedIn: 'root',
})
export class FdUiDialogService {
    private readonly dialog = inject(Dialog);
    private readonly overlay = inject(Overlay);

    public open<T, D = unknown, R = unknown>(component: ComponentType<T>, config: FdUiDialogConfig<D> = {}): FdUiDialogRef<T, R> {
        const size = config.size ?? 'md';
        const providedPanelClasses = this.asArray(config.panelClass);
        const isFullscreen = providedPanelClasses.includes('fd-ui-dialog-panel--fullscreen');
        const isEdgeMobile = this.isCompactMobile() && !isFullscreen;
        const mobileClasses = isEdgeMobile ? ['fd-ui-dialog-panel--edge-mobile'] : [];
        const panelClass = this.mergeClasses(config.panelClass, ['fd-ui-dialog-panel', `fd-ui-dialog-panel--${size}`, ...mobileClasses]);
        const backdropClass = this.mergeClasses(config.backdropClass, ['fd-ui-dialog-backdrop']);
        const positionStrategy =
            config.positionStrategy ?? (isEdgeMobile ? this.overlay.position().global().centerHorizontally().bottom('0') : undefined);
        const baseProviders = config.providers ?? [];

        const dialogConfig: DialogConfig<D, import('@angular/cdk/dialog').DialogRef<R, T>> = {
            ...config,
            width: config.width ?? (isEdgeMobile ? '100vw' : undefined),
            maxWidth: config.maxWidth ?? (isEdgeMobile ? '100vw' : undefined),
            positionStrategy,
            panelClass,
            backdropClass,
            autoFocus: config.autoFocus ?? 'first-tabbable',
            providers: cdkDialogRef => {
                const wrappedRef = new FdUiDialogRef<T, R>(cdkDialogRef);
                return [{ provide: FdUiDialogRef, useValue: wrappedRef }, ...baseProviders] as StaticProvider[];
            },
        };

        const dialogRef = this.dialog.open<R, D, T>(component, dialogConfig);

        return new FdUiDialogRef<T, R>(dialogRef);
    }

    private mergeClasses(provided: string | string[] | undefined, base: string[]): string[] {
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
