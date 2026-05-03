import { Dialog, DialogConfig } from '@angular/cdk/dialog';
import { Overlay } from '@angular/cdk/overlay';
import { ComponentType } from '@angular/cdk/portal';
import { inject, Injectable } from '@angular/core';
import { StaticProvider } from '@angular/core';

import { FdUiDialogSize } from './fd-ui-dialog.component';
import { FdUiDialogRef } from './fd-ui-dialog-ref';

export type FdUiDialogPreset = 'confirm' | 'form' | 'list' | 'detail' | 'fullscreen';

const DEFAULT_DIALOG_TOP_OFFSET = '40px';

export interface FdUiDialogConfig<D = unknown> extends Omit<DialogConfig<D>, 'providers' | 'container'> {
    preset?: FdUiDialogPreset;
    size?: FdUiDialogSize;
    providers?: StaticProvider[];
}

interface ResolvedDialogPreset {
    size?: FdUiDialogSize;
    panelClass?: string[];
    backdropClass?: string[];
}

@Injectable({
    providedIn: 'root',
})
export class FdUiDialogService {
    private readonly dialog = inject(Dialog);
    private readonly overlay = inject(Overlay);

    public open<T, D = unknown, R = unknown>(component: ComponentType<T>, config: FdUiDialogConfig<D> = {}): FdUiDialogRef<T, R> {
        const resolvedPreset = this.resolvePreset(config.preset);
        const presetPanelClasses = resolvedPreset.panelClass ?? [];
        const presetBackdropClasses = resolvedPreset.backdropClass ?? [];
        const size = config.size ?? resolvedPreset.size ?? 'md';
        const providedPanelClasses = this.asArray(config.panelClass);
        const isFullscreen = [...presetPanelClasses, ...providedPanelClasses].includes('fd-ui-dialog-panel--fullscreen');
        const isEdgeMobile = this.isCompactMobile() && !isFullscreen;
        const mobileClasses = isEdgeMobile ? ['fd-ui-dialog-panel--edge-mobile'] : [];
        const panelClass = this.mergeClasses(config.panelClass, [
            ...presetPanelClasses,
            'fd-ui-dialog-panel',
            `fd-ui-dialog-panel--${size}`,
            ...mobileClasses,
        ]);
        const backdropClass = this.mergeClasses(config.backdropClass, [...presetBackdropClasses, 'fd-ui-dialog-backdrop']);
        const positionStrategy =
            config.positionStrategy ??
            (isFullscreen
                ? undefined
                : isEdgeMobile
                  ? this.overlay.position().global().centerHorizontally().bottom('0')
                  : this.overlay.position().global().centerHorizontally().top(DEFAULT_DIALOG_TOP_OFFSET));
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

    private resolvePreset(preset: FdUiDialogPreset | undefined): ResolvedDialogPreset {
        switch (preset) {
            case 'confirm':
                return { size: 'sm' };
            case 'form':
                return { size: 'md' };
            case 'list':
                return { size: 'lg' };
            case 'detail':
                return {
                    size: 'lg',
                    panelClass: ['fd-ui-dialog-panel--detail'],
                    backdropClass: ['fd-ui-dialog-backdrop--detail'],
                };
            case 'fullscreen':
                return {
                    size: 'xl',
                    panelClass: ['fd-ui-dialog-panel--fullscreen'],
                };
            default:
                return {};
        }
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
