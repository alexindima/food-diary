import { DOCUMENT, isPlatformBrowser, type Location, type LocationChangeEvent } from '@angular/common';
import { DestroyRef, inject, InjectionToken, PLATFORM_ID } from '@angular/core';
import { type Observable, Subscription, take } from 'rxjs';

const DIALOG_HISTORY_KEY = '__fdUiDialog';
let nextHistoryId = 0;

type DialogHistoryEntry = {
    id: string;
    url: string;
    state: unknown;
};

type HistoryDialog = {
    readonly disableClose: boolean | undefined;
    readonly closed: Observable<unknown>;
    close: () => void;
};

function readHistoryValue(state: unknown): unknown {
    if (state === null || typeof state !== 'object' || !(DIALOG_HISTORY_KEY in state)) {
        return undefined;
    }
    return state[DIALOG_HISTORY_KEY];
}

function readEntry(state: unknown): DialogHistoryEntry | undefined {
    const entry = readHistoryValue(state);
    if (entry === null || typeof entry !== 'object' || !('id' in entry) || !('url' in entry) || !('state' in entry)) {
        return undefined;
    }
    return typeof entry.id === 'string' && typeof entry.url === 'string' ? { id: entry.id, url: entry.url, state: entry.state } : undefined;
}

/** Owns one same-URL history entry for the entire stack, keeping dialog dismissal out of the router. */
export class FdUiDialogHistory {
    private readonly dialogs: HistoryDialog[] = [];
    private readonly subscriptions = new Subscription();
    private readonly view: Window | null;
    private stopUrlListener: (() => void) | undefined;
    private entry: DialogHistoryEntry | undefined;
    private pendingBack: DialogHistoryEntry | undefined;
    private staleForward: DialogHistoryEntry | undefined;
    private cleanupTimer: number | undefined;
    private handlingBack = false;
    private destroyed = false;
    private lastUrl: string | undefined;
    private pageWasPushed = false;

    public constructor(view: Window | null) {
        this.view = typeof view?.history.pushState === 'function' ? view : null;
        this.lastUrl = this.view?.location.href;
        this.staleForward = readEntry(this.view?.history.state);
    }

    public connect(location: Location | null): void {
        if (this.stopUrlListener !== undefined) {
            return;
        }
        this.stopUrlListener = location?.onUrlChange(() => {
            this.lastUrl = this.view?.location.href;
        });
    }

    public track(dialog: HistoryDialog): void {
        if (this.view === null) {
            return;
        }
        this.dialogs.push(dialog);
        this.subscriptions.add(
            dialog.closed.pipe(take(1)).subscribe({
                complete: () => {
                    this.remove(dialog);
                },
            }),
        );
        this.arm();
    }

    public recordPagePush(): void {
        this.pageWasPushed = true;
    }

    public destroy(): void {
        this.destroyed = true;
        this.stopUrlListener?.();
        this.view?.clearTimeout(this.cleanupTimer);
        this.subscriptions.unsubscribe();
        if (this.view !== null && this.entry !== undefined && this.entry.id === readEntry(this.view.history.state)?.id) {
            this.view.history.replaceState(this.entry.state, '');
        }
    }

    private arm(): void {
        const view = this.view;
        if (view === null || this.dialogs.length === 0 || this.entry !== undefined || this.pendingBack !== undefined || this.handlingBack) {
            return;
        }
        const previousEntry = readEntry(view.history.state);
        const state: unknown = previousEntry === undefined ? view.history.state : previousEntry.state;
        const entry = { id: `${Date.now()}-${++nextHistoryId}`, url: view.location.href, state };
        const pageState = state !== null && typeof state === 'object' ? state : {};
        const dialogState = { ...pageState, [DIALOG_HISTORY_KEY]: entry };
        if (previousEntry === undefined) {
            view.history.pushState(dialogState, '', entry.url);
        } else {
            view.history.replaceState(dialogState, '', entry.url);
        }
        this.entry = entry;
        this.staleForward = undefined;
        this.lastUrl = entry.url;
    }

    private remove(dialog: HistoryDialog): void {
        const index = this.dialogs.indexOf(dialog);
        if (index !== -1) {
            this.dialogs.splice(index, 1);
        }
        if (this.destroyed || this.handlingBack || this.dialogs.length > 0 || this.cleanupTimer !== undefined) {
            return;
        }
        // afterClosed callbacks may open another dialog or navigate. Let those finish before removing our entry.
        this.cleanupTimer = this.view?.setTimeout(() => {
            this.cleanupTimer = undefined;
            this.release();
        }, 0);
    }

    private release(): void {
        if (this.dialogs.length > 0 || this.view === null || this.entry === undefined) {
            return;
        }
        if (readEntry(this.view.history.state)?.id === this.entry.id) {
            this.pendingBack = this.entry;
            this.view.history.back();
        }
        this.entry = undefined;
    }

    public handlePopState(event: LocationChangeEvent): boolean {
        const view = this.view;
        if (view === null || this.destroyed || event.type !== 'popstate') {
            return false;
        }
        return this.handleTraversal(view, event.state);
    }

    private handleTraversal(view: Window, state: unknown): boolean {
        const pageWasPushed = this.pageWasPushed;
        this.pageWasPushed = false;
        const target = readEntry(state);
        const pending = this.pendingBack;
        this.pendingBack = undefined;
        if (target === undefined && pending?.url === view.location.href) {
            this.arm();
            return true;
        }
        const entry = this.entry;
        this.entry = undefined;
        if (target === undefined && entry?.url === view.location.href) {
            this.dismissTop();
            return true;
        }
        if (target !== undefined) {
            this.skipStaleEntry(target, pageWasPushed);
            return true;
        }
        return this.finishTraversal();
    }

    private finishTraversal(): boolean {
        if (this.staleForward?.url === this.view?.location.href && this.staleForward !== undefined) {
            this.staleForward = undefined;
            this.view?.history.back();
            return true;
        }
        this.staleForward = undefined;
        this.lastUrl = this.view?.location.href;
        return false;
    }

    private dismissTop(): void {
        this.handlingBack = true;
        const top = this.dialogs.at(-1);
        if (top !== undefined && top.disableClose !== true) {
            top.close();
        }
        this.handlingBack = false;
        this.arm();
    }

    private skipStaleEntry(entry: DialogHistoryEntry, pageWasPushed: boolean): void {
        if (!pageWasPushed && this.lastUrl === entry.url) {
            this.staleForward = entry;
            this.view?.history.forward();
        } else {
            this.staleForward = undefined;
            this.view?.history.back();
        }
    }
}

export const FD_UI_DIALOG_HISTORY = new InjectionToken<FdUiDialogHistory>('FD_UI_DIALOG_HISTORY', {
    providedIn: 'root',
    factory: (): FdUiDialogHistory => {
        const ownerDocument = inject(DOCUMENT);
        const history = new FdUiDialogHistory(isPlatformBrowser(inject(PLATFORM_ID)) ? ownerDocument.defaultView : null);
        inject(DestroyRef).onDestroy(() => {
            history.destroy();
        });
        return history;
    },
});
