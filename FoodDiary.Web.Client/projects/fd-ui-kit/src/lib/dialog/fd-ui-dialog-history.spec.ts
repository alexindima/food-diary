import type { Location } from '@angular/common';
import { Subject } from 'rxjs';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';

import { FdUiDialogHistory } from './fd-ui-dialog-history';

class TestDialog {
    public disableClose: boolean | undefined = false;
    public readonly closed = new Subject<unknown>();
    public readonly close = vi.fn(() => {
        this.closed.next(undefined);
        this.closed.complete();
    });
}

type HistoryTestContext = {
    history: FdUiDialogHistory;
    view: Window;
    navigated: ReturnType<typeof vi.fn>;
    back: ReturnType<typeof vi.fn>;
    replaceState: ReturnType<typeof vi.fn>;
    navigate: (url: string) => void;
    open: () => TestDialog;
};

function connectEvents(events: EventTarget, history: FdUiDialogHistory, navigated: () => void): void {
    events.addEventListener('popstate', event => {
        if (event instanceof PopStateEvent) {
            const state: unknown = event.state;
            if (!history.handlePopState({ type: event.type, state })) {
                navigated();
            }
        }
    });
}

function setupHistory(): HistoryTestContext {
    const events = new EventTarget();
    const entries: Array<{ url: string; state: unknown }> = [
        { url: 'https://fooddiary.test/previous', state: { navigationId: 1 } },
        { url: 'https://fooddiary.test/current?date=2026-10-24#details', state: { navigationId: 2, editor: 'draft' } },
    ];
    let index = 1;
    let urlChanged: (() => void) | undefined;
    const current = (): (typeof entries)[number] => entries[index];
    const traverse = (delta: number): void => {
        setTimeout(() => {
            const target = index + delta;
            if (target >= 0 && target < entries.length) {
                index = target;
                events.dispatchEvent(new PopStateEvent('popstate', { state: current().state }));
            }
        }, 0);
    };
    const back = vi.fn(() => {
        traverse(-1);
    });
    const replaceState = vi.fn((state: unknown) => {
        current().state = state;
    });
    const view = {
        location: {
            get href(): string {
                return current().url;
            },
        },
        history: {
            get state(): unknown {
                return current().state;
            },
            pushState: vi.fn((state: unknown, _title: string, url: string = current().url) => {
                entries.splice(index + 1, entries.length, { url, state });
                index++;
            }),
            replaceState,
            back,
            forward: vi.fn(() => {
                traverse(1);
            }),
        },
        setTimeout,
        clearTimeout,
    } as unknown as Window;
    const navigated = vi.fn(() => {
        urlChanged?.();
    });
    const location = {
        onUrlChange: (listener: () => void): (() => void) => {
            urlChanged = listener;
            return () => {
                urlChanged = undefined;
            };
        },
    } as unknown as Location;
    const history = new FdUiDialogHistory(view);
    history.connect(location);
    connectEvents(events, history, navigated);
    return {
        history,
        view,
        navigated,
        back,
        replaceState,
        navigate: (url): void => {
            view.history.pushState({ navigationId: 3 }, '', url);
            history.recordPagePush();
            urlChanged?.();
        },
        open: (): TestDialog => {
            const dialog = new TestDialog();
            history.track(dialog);
            return dialog;
        },
    };
}

let context: ReturnType<typeof setupHistory>;

beforeEach(() => {
    vi.useFakeTimers();
    context = setupHistory();
});

afterEach(() => {
    context.history.destroy();
    vi.useRealTimers();
});

describe('dialog Back dismissal', () => {
    it('closes a dialog first, preserving the route, then lets the next Back navigate', () => {
        const dialog = context.open();
        context.view.history.back();
        vi.runAllTimers();

        expect(dialog.close).toHaveBeenCalledOnce();
        expect(context.view.location.href).toBe('https://fooddiary.test/current?date=2026-10-24#details');
        expect(context.view.history.state).toEqual({ navigationId: 2, editor: 'draft' });
        expect(context.navigated).not.toHaveBeenCalled();

        context.view.history.back();
        vi.runAllTimers();
        expect(context.view.location.href).toBe('https://fooddiary.test/previous');
        expect(context.navigated).toHaveBeenCalledOnce();
    });

    it('closes nested dialogs one at a time without notifying router or overlay navigation listeners', () => {
        const parent = context.open();
        const child = context.open();
        context.view.history.back();
        vi.runAllTimers();
        expect(child.close).toHaveBeenCalledOnce();
        expect(parent.close).not.toHaveBeenCalled();

        context.view.history.back();
        vi.runAllTimers();
        expect(parent.close).toHaveBeenCalledOnce();
        expect(context.navigated).not.toHaveBeenCalled();
    });

    it('keeps a locked top dialog and the underlying page open until dismissal is enabled', () => {
        const parent = context.open();
        const child = context.open();
        child.disableClose = true;
        context.view.history.back();
        vi.runAllTimers();
        expect(child.close).not.toHaveBeenCalled();
        expect(parent.close).not.toHaveBeenCalled();
        expect(context.navigated).not.toHaveBeenCalled();

        child.disableClose = false;
        context.view.history.back();
        vi.runAllTimers();
        expect(child.close).toHaveBeenCalledOnce();
        expect(parent.close).not.toHaveBeenCalled();
    });
});

describe('dialog explicit close history cleanup', () => {
    it('removes the temporary step when the final dialog closes through its UI', () => {
        context.open().close();
        vi.runAllTimers();
        expect(context.navigated).not.toHaveBeenCalled();

        context.view.history.back();
        vi.runAllTimers();
        expect(context.view.location.href).toBe('https://fooddiary.test/previous');
        expect(context.navigated).toHaveBeenCalledOnce();
    });

    it('keeps the parent registered after the child closes through its UI', () => {
        const parent = context.open();
        context.open().close();
        vi.runAllTimers();
        context.view.history.back();
        vi.runAllTimers();
        expect(parent.close).toHaveBeenCalledOnce();
        expect(context.navigated).not.toHaveBeenCalled();
    });

    it('handles a parent closing before its child', () => {
        const parent = context.open();
        const child = context.open();
        parent.close();
        vi.runAllTimers();
        context.view.history.back();
        vi.runAllTimers();
        expect(child.close).toHaveBeenCalledOnce();
        expect(context.navigated).not.toHaveBeenCalled();
    });

    it('removes only one history step when all dialogs close together', () => {
        const parent = context.open();
        const child = context.open();
        parent.close();
        child.close();
        vi.runAllTimers();
        expect(context.back).toHaveBeenCalledOnce();
        expect(context.navigated).not.toHaveBeenCalled();
        expect(context.view.history.state).toEqual({ navigationId: 2, editor: 'draft' });
    });
});

describe('dialog close callback history', () => {
    it('keeps a replacement opened by an afterClosed callback dismissible', () => {
        const first = context.open();
        let replacement: TestDialog | undefined;
        first.closed.subscribe(() => {
            replacement = context.open();
        });
        first.close();
        vi.runAllTimers();
        context.view.history.back();
        vi.runAllTimers();
        expect(replacement?.close).toHaveBeenCalledOnce();
        expect(context.navigated).not.toHaveBeenCalled();
    });

    it('re-arms a dialog opened while explicit-close history cleanup is in flight', () => {
        context.open().close();
        vi.runOnlyPendingTimers();
        const replacement = context.open();
        vi.runAllTimers();
        expect(replacement.close).not.toHaveBeenCalled();
        context.view.history.back();
        vi.runAllTimers();
        expect(replacement.close).toHaveBeenCalledOnce();
        expect(context.navigated).not.toHaveBeenCalled();
    });

    it('does not undo navigation performed by an afterClosed callback', () => {
        const dialog = context.open();
        dialog.closed.subscribe(() => {
            context.navigate('https://fooddiary.test/next');
        });
        dialog.close();
        vi.runAllTimers();
        expect(context.view.location.href).toBe('https://fooddiary.test/next');

        context.view.history.back();
        vi.runAllTimers();
        expect(context.view.location.href).toBe('https://fooddiary.test/current?date=2026-10-24#details');
        expect(context.navigated).toHaveBeenCalledOnce();
        context.view.history.back();
        vi.runAllTimers();
        expect(context.view.location.href).toBe('https://fooddiary.test/previous');
    });
});

describe('expired dialog history', () => {
    it('skips a stale dialog entry when a later page state has the same URL', () => {
        context.open().close();
        context.navigate(context.view.location.href);
        vi.runAllTimers();
        context.view.history.back();
        vi.runAllTimers();
        expect(context.view.history.state).toEqual({ navigationId: 2, editor: 'draft' });
        expect(context.navigated).toHaveBeenCalledOnce();
        context.view.history.back();
        vi.runAllTimers();
        expect(context.view.location.href).toBe('https://fooddiary.test/previous');
    });

    it('skips expired dialog steps in both Back and Forward navigation', () => {
        context.open().close();
        context.navigate('https://fooddiary.test/next');
        vi.runAllTimers();
        context.view.history.back();
        vi.runAllTimers();
        context.view.history.forward();
        vi.runAllTimers();
        expect(context.view.location.href).toBe('https://fooddiary.test/next');
        expect(context.navigated).toHaveBeenCalledTimes(2);
    });

    it('does not reopen a dismissed dialog with Forward or require an extra Back afterwards', () => {
        const dialog = context.open();
        context.view.history.back();
        vi.runAllTimers();
        context.view.history.forward();
        vi.runAllTimers();
        expect(dialog.close).toHaveBeenCalledOnce();
        context.view.history.back();
        vi.runAllTimers();
        expect(context.view.location.href).toBe('https://fooddiary.test/previous');
        expect(context.navigated).toHaveBeenCalledOnce();
    });

    it('reuses an expired forward step when another dialog opens there', () => {
        context.open();
        context.view.history.back();
        vi.runAllTimers();
        context.view.history.forward();
        vi.runAllTimers();
        const dialog = context.open();
        context.view.history.back();
        vi.runAllTimers();
        expect(dialog.close).toHaveBeenCalledOnce();
        context.view.history.back();
        vi.runAllTimers();
        expect(context.view.location.href).toBe('https://fooddiary.test/previous');
    });
});

describe('dialog history lifecycle', () => {
    it('does not alter route state when destroyed without any dialogs', () => {
        context.history.destroy();
        expect(context.view.history.state).toEqual({ navigationId: 2, editor: 'draft' });
        expect(context.replaceState).not.toHaveBeenCalled();
    });

    it('restores route state and removes its listener when destroyed with an open dialog', () => {
        const dialog = context.open();
        context.history.destroy();
        expect(context.view.history.state).toEqual({ navigationId: 2, editor: 'draft' });
        context.view.history.back();
        vi.runAllTimers();
        expect(dialog.close).not.toHaveBeenCalled();
        expect(context.navigated).toHaveBeenCalledOnce();
    });

    it('does no browser work during server rendering', () => {
        const history = new FdUiDialogHistory(null);
        const dialog = new TestDialog();
        expect(() => {
            history.track(dialog);
            history.destroy();
        }).not.toThrow();
        expect(dialog.close).not.toHaveBeenCalled();
    });
});
