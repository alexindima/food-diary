import { Dialog, type DialogConfig, type DialogRef } from '@angular/cdk/dialog';
import { Overlay } from '@angular/cdk/overlay';
import type { ComponentType } from '@angular/cdk/portal';
import type { StaticProvider } from '@angular/core';
import { inject, Injectable } from '@angular/core';

import type { FdUiDialogSize } from './fd-ui-dialog.component';
import { FD_UI_DIALOG_COMPACT_VIEWPORT_QUERY } from './fd-ui-dialog.tokens';
import { FdUiDialogRef } from './fd-ui-dialog-ref';

export type FdUiDialogPreset = 'confirm' | 'form' | 'list' | 'detail' | 'fullscreen';

const DEFAULT_DIALOG_TOP_OFFSET = '40px';

export type FdUiDialogConfig<D = unknown> = {
    preset?: FdUiDialogPreset;
    size?: FdUiDialogSize;
    providers?: StaticProvider[];
} & Omit<DialogConfig<D>, 'providers' | 'container'>;

type ResolvedDialogPreset = {
    size?: FdUiDialogSize;
    panelClass?: string[];
    backdropClass?: string[];
};

type ResolvedDialogLayout = {
    panelClass: string[];
    backdropClass: string[];
    isEdgeMobile: boolean;
    isFullscreen: boolean;
};

@Injectable({
    providedIn: 'root',
})
export class FdUiDialogService {
    private readonly dialog = inject(Dialog);
    private readonly overlay = inject(Overlay);
    private readonly compactViewportQuery = inject(FD_UI_DIALOG_COMPACT_VIEWPORT_QUERY);

    public open<T, D = unknown, R = unknown>(component: ComponentType<T>, config: FdUiDialogConfig<D> = {}): FdUiDialogRef<T, R> {
        const layout = this.resolveDialogLayout(config);
        const size = this.resolveDialogSize(config);
        const positionStrategy = config.positionStrategy ?? this.resolvePositionStrategy(layout);
        const baseProviders = config.providers ?? [];

        const dialogConfig: DialogConfig<D, DialogRef<R, T>> = {
            ...config,
            width: config.width ?? this.resolveDialogWidth(layout, size),
            maxWidth: config.maxWidth ?? (layout.isEdgeMobile ? '100vw' : undefined),
            positionStrategy,
            panelClass: this.withSizeClass(layout.panelClass, size),
            backdropClass: layout.backdropClass,
            autoFocus: config.autoFocus ?? 'first-tabbable',
            providers: cdkDialogRef => {
                const wrappedRef = new FdUiDialogRef<T, R>(cdkDialogRef);
                return [{ provide: FdUiDialogRef, useValue: wrappedRef }, ...baseProviders] as StaticProvider[];
            },
        };

        const dialogRef = this.dialog.open<R, D, T>(component, dialogConfig);

        return new FdUiDialogRef<T, R>(dialogRef);
    }

    private resolveDialogSize(config: FdUiDialogConfig): FdUiDialogSize {
        return config.size ?? this.resolvePreset(config.preset).size ?? 'md';
    }

    private resolveDialogLayout(config: FdUiDialogConfig): ResolvedDialogLayout {
        const resolvedPreset = this.resolvePreset(config.preset);
        const presetPanelClasses = resolvedPreset.panelClass ?? [];
        const providedPanelClasses = this.asArray(config.panelClass);
        const isFullscreen = [...presetPanelClasses, ...providedPanelClasses].includes('fd-ui-dialog-panel--fullscreen');
        const isEdgeMobile = this.isCompactMobile() && !isFullscreen;

        return {
            panelClass: this.mergeClasses(config.panelClass, [
                ...presetPanelClasses,
                'fd-ui-dialog-panel',
                ...(isEdgeMobile ? ['fd-ui-dialog-panel--edge-mobile'] : []),
            ]),
            backdropClass: this.mergeClasses(config.backdropClass, [...(resolvedPreset.backdropClass ?? []), 'fd-ui-dialog-backdrop']),
            isEdgeMobile,
            isFullscreen,
        };
    }

    private withSizeClass(panelClass: string[], size: FdUiDialogSize): string[] {
        return [...panelClass, `fd-ui-dialog-panel--${size}`];
    }

    private resolveDialogWidth(layout: ResolvedDialogLayout, size: FdUiDialogSize): string | undefined {
        if (layout.isFullscreen) {
            return undefined;
        }

        if (layout.isEdgeMobile) {
            return '100vw';
        }

        return `min(calc(100vw - var(--fd-size-dialog-panel-width-offset)), var(--fd-size-dialog-panel-width-${size}))`;
    }

    private resolvePositionStrategy(layout: ResolvedDialogLayout): DialogConfig['positionStrategy'] {
        if (layout.isFullscreen) {
            return undefined;
        }

        return layout.isEdgeMobile
            ? this.overlay.position().global().centerHorizontally().bottom('0')
            : this.overlay.position().global().centerHorizontally().top(DEFAULT_DIALOG_TOP_OFFSET);
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
            case undefined:
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
        if (classes === undefined || classes.length === 0) {
            return [];
        }
        return Array.isArray(classes) ? classes : [classes];
    }

    private isCompactMobile(): boolean {
        return typeof window !== 'undefined' && window.matchMedia(this.compactViewportQuery).matches;
    }
}
