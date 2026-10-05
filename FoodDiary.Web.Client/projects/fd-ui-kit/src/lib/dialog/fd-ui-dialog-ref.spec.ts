import type { DialogCloseOptions, DialogRef } from '@angular/cdk/dialog';
import { Subject } from 'rxjs';
import { describe, expect, it, vi } from 'vitest';

import { FdUiDialogRef } from './fd-ui-dialog-ref';

type DialogComponent = {
    readonly title: string;
};

type DialogRefMock = {
    addPanelClass: ReturnType<typeof vi.fn<(classes: string | string[]) => void>>;
    backdropClick: Subject<MouseEvent>;
    close: ReturnType<typeof vi.fn<(result?: string, options?: DialogCloseOptions) => void>>;
    closed: Subject<string | undefined>;
    componentInstance: DialogComponent;
    componentRef: null;
    disableClose?: boolean;
    id: string;
    keydownEvents: Subject<KeyboardEvent>;
    overlayRef: unknown;
    removePanelClass: ReturnType<typeof vi.fn<(classes: string | string[]) => void>>;
    updatePosition: ReturnType<typeof vi.fn<() => void>>;
    updateSize: ReturnType<typeof vi.fn<(width?: string | number, height?: string | number) => void>>;
};

function createDialogRefMock(): DialogRefMock {
    return {
        addPanelClass: vi.fn<(classes: string | string[]) => void>(),
        backdropClick: new Subject<MouseEvent>(),
        close: vi.fn<(result?: string, options?: DialogCloseOptions) => void>(),
        closed: new Subject<string | undefined>(),
        componentInstance: { title: 'Dialog' },
        componentRef: null,
        disableClose: false,
        id: 'dialog-1',
        keydownEvents: new Subject<KeyboardEvent>(),
        overlayRef: {},
        removePanelClass: vi.fn<(classes: string | string[]) => void>(),
        updatePosition: vi.fn<() => void>(),
        updateSize: vi.fn<(width?: string | number, height?: string | number) => void>(),
    };
}

function createRef(mock = createDialogRefMock()): {
    mock: DialogRefMock;
    ref: FdUiDialogRef<DialogComponent, string>;
} {
    return {
        mock,
        ref: new FdUiDialogRef<DialogComponent, string>(mock as unknown as DialogRef<string, DialogComponent>),
    };
}

describe('FdUiDialogRef', () => {
    it('exposes wrapped dialog state', () => {
        const { mock, ref } = createRef();

        expect(ref.id).toBe('dialog-1');
        expect(ref.componentInstance).toEqual({ title: 'Dialog' });
        expect(ref.componentRef).toBeNull();
        expect(ref.overlayRef).toBe(mock.overlayRef);
        expect(ref.disableClose).toBe(false);
    });

    it('updates disableClose on the wrapped dialog ref', () => {
        const { mock, ref } = createRef();

        ref.disableClose = true;

        expect(mock.disableClose).toBe(true);
        expect(ref.disableClose).toBe(true);
    });

    it('delegates close with result and options', () => {
        const { mock, ref } = createRef();
        const options: DialogCloseOptions = { focusOrigin: 'keyboard' };

        ref.close('confirmed', options);

        expect(mock.close).toHaveBeenCalledWith('confirmed', options);
    });

    it('returns chainable wrappers for mutable operations', () => {
        const { mock, ref } = createRef();

        expect(ref.updatePosition()).toBe(ref);
        expect(ref.updateSize('320px', '240px')).toBe(ref);
        expect(ref.addPanelClass(['wide', 'accent'])).toBe(ref);
        expect(ref.removePanelClass('accent')).toBe(ref);

        expect(mock.updatePosition).toHaveBeenCalledOnce();
        expect(mock.updateSize).toHaveBeenCalledWith('320px', '240px');
        expect(mock.addPanelClass).toHaveBeenCalledWith(['wide', 'accent']);
        expect(mock.removePanelClass).toHaveBeenCalledWith('accent');
    });

    it('exposes dialog observables', () => {
        const { mock, ref } = createRef();
        const closedSpy = vi.fn();
        const afterClosedSpy = vi.fn();
        const backdropSpy = vi.fn();
        const keydownSpy = vi.fn();
        ref.closed.subscribe(closedSpy);
        ref.afterClosed().subscribe(afterClosedSpy);
        ref.backdropClick.subscribe(backdropSpy);
        ref.keydownEvents.subscribe(keydownSpy);

        const mouseEvent = new MouseEvent('click');
        const keyboardEvent = new KeyboardEvent('keydown', { key: 'Escape' });
        mock.backdropClick.next(mouseEvent);
        mock.keydownEvents.next(keyboardEvent);
        mock.closed.next('confirmed');

        expect(backdropSpy).toHaveBeenCalledWith(mouseEvent);
        expect(keydownSpy).toHaveBeenCalledWith(keyboardEvent);
        expect(closedSpy).toHaveBeenCalledWith('confirmed');
        expect(afterClosedSpy).toHaveBeenCalledWith('confirmed');
    });
});

describe('FdUiDialogRef dismissal locks', () => {
    it.each([true, false, undefined])('restores the configured policy %s after release', policy => {
        const mock = createDialogRefMock();
        mock.disableClose = policy;
        const { ref } = createRef(mock);
        const release = ref.acquireDismissalLock();
        expect(mock.disableClose).toBe(true);
        release();
        expect(mock.disableClose).toBe(policy);
    });

    it('keeps independent locks active and releases each only once', () => {
        const { ref, mock } = createRef();
        const first = ref.acquireDismissalLock();
        const second = ref.acquireDismissalLock();
        first();
        first();
        expect(mock.disableClose).toBe(true);
        second();
        expect(mock.disableClose).toBe(false);
    });

    it.each([true, false])('preserves a changed ref policy %s while locked', policy => {
        const { ref, mock } = createRef();
        const release = ref.acquireDismissalLock();
        ref.disableClose = policy;
        expect(mock.disableClose).toBe(true);
        release();
        expect(mock.disableClose).toBe(policy);
    });

    it('shares dismissal state between provider and returned wrappers', () => {
        const { ref, mock } = createRef();
        const returned = createRef(mock).ref;
        const release = ref.acquireDismissalLock();
        returned.disableClose = true;
        release();
        expect(returned.disableClose).toBe(true);
        const nextRelease = returned.acquireDismissalLock();
        ref.disableClose = false;
        expect(mock.disableClose).toBe(true);
        nextRelease();
        expect(mock.disableClose).toBe(false);
    });

    it('allows an intentional close while dismissal is locked', () => {
        const { ref, mock } = createRef();
        ref.acquireDismissalLock();
        ref.close('saved');
        expect(mock.close).toHaveBeenCalledWith('saved', undefined);
    });

    it('keeps separate dialogs independent', () => {
        const first = createRef();
        const second = createRef();
        first.ref.acquireDismissalLock();
        expect(second.ref.disableClose).toBe(false);
    });
});
