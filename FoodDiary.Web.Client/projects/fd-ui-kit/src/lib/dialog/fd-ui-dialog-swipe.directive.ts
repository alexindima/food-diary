import { DOCUMENT } from '@angular/common';
import { afterNextRender, DestroyRef, Directive, effect, ElementRef, inject, input } from '@angular/core';

import { FD_UI_DIALOG_COMPACT_VIEWPORT_QUERY } from './fd-ui-dialog.tokens';
import { FdUiDialogRef } from './fd-ui-dialog-ref';

const DIRECTION_SLOP = 8;
const FLICK_DISTANCE = 40;
const FLICK_VELOCITY = 0.6; // CSS pixels per millisecond.
const SETTLE_DURATION = 180;
const MIN_DISMISS_DISTANCE = 48;
const DISMISS_HEIGHT_RATIO = 0.25;
const VERTICAL_DIRECTION_RATIO = 1.2;
const MAX_FLICK_AGE = 100;
const INTERACTIVE =
    'button, a, input, textarea, select, [contenteditable]:not([contenteditable="false"]), [role="slider"], [data-fd-dialog-swipe-ignore], .fd-ui-dialog__footer';

type Swipe = {
    identifier: number;
    target: HTMLElement;
    startX: number;
    startY: number;
    lastY: number;
    lastTime: number;
    velocity: number;
    dragging: boolean;
};

/** Internal gesture owner. Native scrolling keeps ownership for the entire gesture once it starts. */
@Directive({ selector: '[fdUiDialogSwipe]' })
export class FdUiDialogSwipeDirective {
    private readonly element = inject<ElementRef<HTMLElement>>(ElementRef).nativeElement;
    private readonly document = inject(DOCUMENT);
    private readonly dialogRef = inject(FdUiDialogRef, { optional: true });
    private readonly compactQuery = inject(FD_UI_DIALOG_COMPACT_VIEWPORT_QUERY);
    private readonly destroyRef = inject(DestroyRef);
    public readonly disabled = input(false, { alias: 'fdUiDialogSwipeDisabled' });
    private swipe: Swipe | undefined;
    private settleTimer: number | undefined;

    public constructor() {
        effect(() => {
            if (this.disabled()) {
                this.reset();
            }
        });
        afterNextRender(() => {
            const start = (event: TouchEvent): void => {
                this.start(event);
            };
            const move = (event: TouchEvent): void => {
                this.move(event);
            };
            const end = (event: TouchEvent): void => {
                this.end(event);
            };
            const cancel = (): void => {
                this.settle(false);
            };
            this.element.addEventListener('touchstart', start, { passive: true });
            // A non-passive move is necessary to claim only a downward drag at scrollTop=0.
            // Pointer Events with pan-y would be cancelled as soon as native scrolling wins.
            this.element.addEventListener('touchmove', move, { passive: false });
            this.element.addEventListener('touchend', end, { passive: true });
            this.element.addEventListener('touchcancel', cancel, { passive: true });
            this.destroyRef.onDestroy(() => {
                this.element.removeEventListener('touchstart', start);
                this.element.removeEventListener('touchmove', move);
                this.element.removeEventListener('touchend', end);
                this.element.removeEventListener('touchcancel', cancel);
                this.reset();
            });
        });
    }

    private canDismiss(): boolean {
        if (this.disabled() || this.dialogRef === null || this.dialogRef.disableClose === true) {
            return false;
        }
        const panel = this.element.closest('.cdk-overlay-pane');
        const containers = this.document.querySelectorAll('.cdk-dialog-container');
        return (
            panel?.classList.contains('fd-ui-dialog-panel--edge-mobile') === true &&
            this.document.defaultView?.matchMedia(this.compactQuery).matches === true &&
            containers.length > 0 &&
            containers.item(containers.length - 1).id === this.dialogRef.id
        );
    }

    private start(event: TouchEvent): void {
        if (event.touches.length !== 1) {
            this.settle(false);
            return;
        }
        const touch = event.touches[0];
        const target = event.target;
        const elementType = this.document.defaultView?.HTMLElement;
        if (elementType === undefined || !(target instanceof elementType) || this.settleTimer !== undefined || !this.canDismiss()) {
            return;
        }
        if (target.closest(INTERACTIVE) !== null) {
            return;
        }
        if (!this.isAtScrollStart(target)) {
            return;
        }
        this.swipe = {
            identifier: touch.identifier,
            target,
            startX: touch.clientX,
            startY: touch.clientY,
            lastY: touch.clientY,
            lastTime: event.timeStamp,
            velocity: 0,
            dragging: false,
        };
    }

    private isAtScrollStart(target: HTMLElement): boolean {
        for (let node: HTMLElement | null = target; node !== null && node !== this.element; node = node.parentElement) {
            const style = this.document.defaultView?.getComputedStyle(node);
            if (style !== undefined && /auto|scroll/.test(style.overflowY) && node.scrollTop > 0) {
                return false;
            }
        }
        return true;
    }

    private move(event: TouchEvent): void {
        const swipe = this.swipe;
        if (swipe === undefined) {
            return;
        }
        if (!this.canDismiss() || event.touches.length !== 1 || !event.cancelable) {
            this.settle(false);
            return;
        }
        const touch = event.touches[0];
        if (touch.identifier !== swipe.identifier) {
            this.settle(false);
            return;
        }
        const deltaY = touch.clientY - swipe.startY;
        const deltaX = Math.abs(touch.clientX - swipe.startX);
        if (!swipe.dragging && !this.claimDirection(swipe, deltaX, deltaY)) {
            return;
        }
        event.preventDefault();
        swipe.velocity = (touch.clientY - swipe.lastY) / Math.max(1, event.timeStamp - swipe.lastTime);
        swipe.lastY = touch.clientY;
        swipe.lastTime = event.timeStamp;
        this.element.style.translate = `0 ${Math.max(0, deltaY)}px`;
    }

    private claimDirection(swipe: Swipe, deltaX: number, deltaY: number): boolean {
        if (Math.max(deltaX, Math.abs(deltaY)) < DIRECTION_SLOP) {
            return false;
        }
        if (deltaY <= deltaX * VERTICAL_DIRECTION_RATIO || !this.isAtScrollStart(swipe.target)) {
            this.swipe = undefined;
            return false;
        }
        swipe.dragging = true;
        this.element.classList.add('fd-ui-dialog--dragging');
        return true;
    }

    private end(event: TouchEvent): void {
        const swipe = this.swipe;
        if (swipe?.dragging !== true) {
            this.swipe = undefined;
            return;
        }
        const touch = Array.from(event.changedTouches).find(point => point.identifier === swipe.identifier);
        const distance = Math.max(0, (touch?.clientY ?? swipe.lastY) - swipe.startY);
        const threshold = Math.max(MIN_DISMISS_DISTANCE, this.element.getBoundingClientRect().height * DISMISS_HEIGHT_RATIO);
        const flick = distance >= FLICK_DISTANCE && swipe.velocity >= FLICK_VELOCITY && event.timeStamp - swipe.lastTime < MAX_FLICK_AGE;
        this.settle(this.canDismiss() && (distance >= threshold || flick));
    }

    private settle(dismiss: boolean): void {
        if (this.swipe?.dragging !== true) {
            this.swipe = undefined;
            return;
        }
        this.swipe = undefined;
        this.element.classList.remove('fd-ui-dialog--dragging');
        this.element.classList.add('fd-ui-dialog--swipe-settling');
        this.element.style.translate = dismiss ? `0 ${this.element.getBoundingClientRect().height}px` : '0 0';
        const view = this.document.defaultView;
        const finish = (): void => {
            if (dismiss && this.canDismiss()) {
                this.dialogRef?.close();
            }
            // Also return to rest if a closePredicate rejected the dismissal.
            this.reset();
        };
        if (view === null || view.matchMedia('(prefers-reduced-motion: reduce)').matches) {
            finish();
        } else {
            this.settleTimer = view.setTimeout(finish, SETTLE_DURATION);
        }
    }

    private reset(): void {
        this.document.defaultView?.clearTimeout(this.settleTimer);
        this.settleTimer = undefined;
        this.swipe = undefined;
        this.element.classList.remove('fd-ui-dialog--dragging', 'fd-ui-dialog--swipe-settling');
        this.element.style.removeProperty('translate');
    }
}
