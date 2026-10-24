import { Dialog, type DialogConfig, type DialogRef } from '@angular/cdk/dialog';
import { Overlay } from '@angular/cdk/overlay';
import type { ComponentType } from '@angular/cdk/portal';
import { DOCUMENT, isPlatformBrowser, Location } from '@angular/common';
import { inject, PLATFORM_ID, Service, type StaticProvider, type TemplateRef } from '@angular/core';

import type { FdUiDialogSize } from './fd-ui-dialog';
import { FD_UI_DIALOG_COMPACT_VIEWPORT_QUERY, FD_UI_DIALOG_DISMISSAL_LOCK } from './fd-ui-dialog.tokens';
import { FD_UI_DIALOG_BACKGROUND } from './fd-ui-dialog-background';
import { FD_UI_DIALOG_HISTORY } from './fd-ui-dialog-history';
import { FdUiDialogRef } from './fd-ui-dialog-ref';

export type FdUiDialogPreset = 'confirm' | 'form' | 'list' | 'detail' | 'fullscreen' | 'surface';

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

@Service()
export class FdUiDialogService {
    private readonly dialog = inject(Dialog);
    private readonly overlay = inject(Overlay);
    private readonly document = inject(DOCUMENT);
    private readonly isBrowser = isPlatformBrowser(inject(PLATFORM_ID));
    private readonly compactViewportQuery = inject(FD_UI_DIALOG_COMPACT_VIEWPORT_QUERY);
    private readonly history = inject(FD_UI_DIALOG_HISTORY);
    private readonly background = inject(FD_UI_DIALOG_BACKGROUND);

    public constructor() {
        this.history.connect(inject(Location, { optional: true }));
    }

    public closeAll(): void {
        this.dialog.closeAll();
    }

    public open<T, D = unknown, R = unknown>(
        component: ComponentType<T> | TemplateRef<T>,
        config: FdUiDialogConfig<D> = {},
    ): FdUiDialogRef<T, R> {
        const layout = this.resolveDialogLayout(config);
        const size = this.resolveDialogSize(config);
        const positionStrategy = config.positionStrategy ?? this.resolvePositionStrategy(layout);
        const baseProviders = config.providers ?? [];
        let releaseBackground = (): void => {};

        const dialogConfig: DialogConfig<D, DialogRef<R, T>> = {
            ...config,
            width: config.width ?? this.resolveDialogWidth(layout, size),
            maxWidth: config.maxWidth ?? (layout.isEdgeMobile ? '100vw' : undefined),
            positionStrategy,
            panelClass: this.withSizeClass(layout.panelClass, size),
            backdropClass: layout.backdropClass,
            autoFocus: config.autoFocus ?? 'first-tabbable',
            scrollStrategy: config.scrollStrategy ?? this.overlay.scrollStrategies.block(),
            closePredicate: (result, resolvedConfig, instance) => {
                if (config.closePredicate?.(result, resolvedConfig, instance) === false) {
                    return false;
                }
                // CDK restores focus while disposing the container, before `closed` emits.
                releaseBackground();
                return true;
            },
            providers: cdkDialogRef => {
                const wrappedRef = new FdUiDialogRef<T, R>(cdkDialogRef);
                return [
                    { provide: FdUiDialogRef, useValue: wrappedRef },
                    { provide: FD_UI_DIALOG_DISMISSAL_LOCK, useValue: () => wrappedRef.acquireDismissalLock() },
                    ...baseProviders,
                ] as StaticProvider[];
            },
        };

        const dialogRef = this.dialog.open<R, D, T>(component, dialogConfig);
        releaseBackground = this.background.acquire();
        dialogRef.closed.subscribe({
            complete: () => {
                releaseBackground();
            },
        });
        this.history.track(dialogRef);

        return new FdUiDialogRef<T, R>(dialogRef);
    }

    private resolveDialogSize(config: FdUiDialogConfig): FdUiDialogSize {
        return config.size ?? this.resolvePreset(config.preset).size ?? 'md';
    }

    private resolveDialogLayout(config: FdUiDialogConfig): ResolvedDialogLayout {
        const resolvedPreset = this.resolvePreset(config.preset);
        const presetPanelClasses = resolvedPreset.panelClass ?? [];
        const providedPanelClasses = this.asArray(config.panelClass);
        const isFullscreen = [...presetPanelClasses, ...providedPanelClasses].some(
            className => className === 'fd-ui-dialog-panel--fullscreen' || className === 'fd-ui-dialog-panel--surface',
        );
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
            case 'confirm': {
                return { size: 'sm' };
            }
            case 'form': {
                return { size: 'md' };
            }
            case 'list': {
                return { size: 'lg' };
            }
            case 'detail': {
                return {
                    size: 'lg',
                    panelClass: ['fd-ui-dialog-panel--detail'],
                    backdropClass: ['fd-ui-dialog-backdrop--detail'],
                };
            }
            case 'fullscreen': {
                return {
                    size: 'xl',
                    panelClass: ['fd-ui-dialog-panel--fullscreen'],
                };
            }
            case 'surface': {
                return { panelClass: ['fd-ui-dialog-panel--surface'] };
            }
            case undefined: {
                return {};
            }
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
        if (!this.isBrowser) {
            return false;
        }
        const mediaQuery = this.document.defaultView?.matchMedia(this.compactViewportQuery);
        return mediaQuery?.matches === true;
    }
}
