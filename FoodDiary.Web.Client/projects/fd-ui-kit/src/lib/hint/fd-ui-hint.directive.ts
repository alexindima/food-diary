import { type ConnectedPosition, Overlay, type OverlayRef } from '@angular/cdk/overlay';
import { ComponentPortal } from '@angular/cdk/portal';
import { booleanAttribute, DestroyRef, Directive, ElementRef, inject, input, TemplateRef } from '@angular/core';
import { DomSanitizer, type SafeHtml } from '@angular/platform-browser';

import { FdUiHintOverlayComponent } from './fd-ui-hint-overlay.component';

type HintContent = string | TemplateRef<unknown> | null;
type HintPosition = 'top' | 'bottom' | 'left' | 'right';

let nextHintId = 0;

@Directive({
    selector: '[fdUiHint]',
    standalone: true,
    host: {
        '(mouseenter)': 'onMouseEnter()',
        '(mouseleave)': 'onMouseLeave()',
        '(focusin)': 'onFocusIn()',
        '(focusout)': 'onFocusOut()',
        '(click)': 'onClick()',
        '(keydown.escape)': 'onEscape()',
    },
})
export class FdUiHintDirective {
    public readonly fdUiHint = input<HintContent>(null);
    public readonly fdUiHintHtml = input(false);
    public readonly fdUiHintContext = input<Record<string, unknown> | null>(null);
    public readonly fdUiHintShowDelay = input(500);
    public readonly fdUiHintFocusShowDelay = input(0);
    public readonly fdUiHintHideDelay = input(0);
    public readonly fdUiHintPosition = input<HintPosition>('bottom');
    public readonly fdUiHintDisabled = input(false, { transform: booleanAttribute });

    private readonly overlay = inject(Overlay);
    private readonly elementRef = inject(ElementRef<HTMLElement>);
    private readonly sanitizer = inject(DomSanitizer);
    private readonly destroyRef = inject(DestroyRef);
    private readonly tooltipId = `fd-ui-hint-${nextHintId++}`;
    private readonly initialAriaDescribedBy = this.elementRef.nativeElement.getAttribute('aria-describedby');
    private overlayRef: OverlayRef | null = null;
    private showTimeoutId: number | null = null;
    private hideTimeoutId: number | null = null;

    public constructor() {
        this.destroyRef.onDestroy(() => {
            this.clearTimers();
            this.syncAriaDescribedBy(false);
            this.destroyOverlay();
        });
    }

    public onMouseEnter(): void {
        this.queueShow(this.fdUiHintShowDelay());
    }

    public onMouseLeave(): void {
        this.queueHide();
    }

    public onFocusIn(): void {
        if (!this.isKeyboardVisibleFocus()) {
            return;
        }

        this.queueShow(this.fdUiHintFocusShowDelay());
    }

    public onFocusOut(): void {
        this.queueHide();
    }

    public onClick(): void {
        this.cancelPendingDisplay();
        this.hide();
    }

    public onEscape(): void {
        this.cancelPendingDisplay();
        this.hide();
    }

    private isKeyboardVisibleFocus(): boolean {
        const host = this.elementRef.nativeElement;
        return typeof host.matches === 'function' ? host.matches(':focus-visible') : false;
    }

    private queueShow(delay: number): void {
        if (!this.hasContent() || this.fdUiHintDisabled()) {
            return;
        }

        this.clearHideTimer();

        if (this.showTimeoutId !== null) {
            return;
        }

        this.showTimeoutId = window.setTimeout(() => {
            this.showTimeoutId = null;
            this.show();
        }, delay);
    }

    private queueHide(): void {
        this.clearShowTimer();

        if (this.hideTimeoutId !== null) {
            return;
        }

        this.hideTimeoutId = window.setTimeout(() => {
            this.hideTimeoutId = null;
            this.hide();
        }, this.fdUiHintHideDelay());
    }

    private show(): void {
        const content = this.fdUiHint();
        if (!content || this.fdUiHintDisabled()) {
            return;
        }

        this.overlayRef ??= this.createOverlay();

        if (this.overlayRef.hasAttached()) {
            return;
        }

        const portal = new ComponentPortal(FdUiHintOverlayComponent);
        const ref = this.overlayRef.attach(portal);
        ref.setInput('tooltipId', this.tooltipId);
        ref.setInput('contentContext', this.fdUiHintContext());

        if (content instanceof TemplateRef) {
            ref.setInput('contentTemplate', content);
            ref.setInput('contentText', null);
            ref.setInput('contentHtml', null);
        } else if (this.fdUiHintHtml()) {
            ref.setInput('contentTemplate', null);
            ref.setInput('contentText', null);
            ref.setInput('contentHtml', this.toSafeHtml(content));
        } else {
            ref.setInput('contentTemplate', null);
            ref.setInput('contentText', content);
            ref.setInput('contentHtml', null);
        }

        this.syncAriaDescribedBy(true);
    }

    private hide(): void {
        if (!this.overlayRef?.hasAttached()) {
            return;
        }

        this.overlayRef.detach();
        this.syncAriaDescribedBy(false);
    }

    private createOverlay(): OverlayRef {
        const positionStrategy = this.overlay
            .position()
            .flexibleConnectedTo(this.elementRef)
            .withFlexibleDimensions(false)
            .withViewportMargin(8)
            .withPositions(this.getPositions());

        return this.overlay.create({
            positionStrategy,
            scrollStrategy: this.overlay.scrollStrategies.reposition(),
            panelClass: 'fd-ui-hint-panel',
        });
    }

    private getPositions(): ConnectedPosition[] {
        const offset = 6;
        const top: ConnectedPosition[] = [
            {
                originX: 'center',
                originY: 'top',
                overlayX: 'center',
                overlayY: 'bottom',
                offsetY: -offset,
            },
            {
                originX: 'center',
                originY: 'bottom',
                overlayX: 'center',
                overlayY: 'top',
                offsetY: offset,
            },
        ];
        const bottom: ConnectedPosition[] = [
            {
                originX: 'center',
                originY: 'bottom',
                overlayX: 'center',
                overlayY: 'top',
                offsetY: offset,
            },
            {
                originX: 'center',
                originY: 'top',
                overlayX: 'center',
                overlayY: 'bottom',
                offsetY: -offset,
            },
        ];
        const left: ConnectedPosition[] = [
            {
                originX: 'start',
                originY: 'center',
                overlayX: 'end',
                overlayY: 'center',
                offsetX: -offset,
            },
            {
                originX: 'end',
                originY: 'center',
                overlayX: 'start',
                overlayY: 'center',
                offsetX: offset,
            },
        ];
        const right: ConnectedPosition[] = [
            {
                originX: 'end',
                originY: 'center',
                overlayX: 'start',
                overlayY: 'center',
                offsetX: offset,
            },
            {
                originX: 'start',
                originY: 'center',
                overlayX: 'end',
                overlayY: 'center',
                offsetX: -offset,
            },
        ];

        switch (this.fdUiHintPosition()) {
            case 'bottom':
                return bottom;
            case 'left':
                return left;
            case 'right':
                return right;
            default:
                return top;
        }
    }

    private hasContent(): boolean {
        const content = this.fdUiHint();
        if (content instanceof TemplateRef) {
            return true;
        }

        return typeof content === 'string' ? content.trim().length > 0 : Boolean(content);
    }

    private syncAriaDescribedBy(isVisible: boolean): void {
        const host = this.elementRef.nativeElement;
        const tokens = new Set(
            (this.initialAriaDescribedBy ?? '')
                .split(/\s+/)
                .map((token: string) => token.trim())
                .filter(Boolean),
        );

        if (isVisible) {
            tokens.add(this.tooltipId);
        } else {
            tokens.delete(this.tooltipId);
        }

        const value = Array.from(tokens).join(' ');
        if (value) {
            host.setAttribute('aria-describedby', value);
            return;
        }

        host.removeAttribute('aria-describedby');
    }

    private toSafeHtml(content: string): SafeHtml {
        return this.sanitizer.bypassSecurityTrustHtml(content);
    }

    private clearTimers(): void {
        this.clearShowTimer();
        this.clearHideTimer();
    }

    private cancelPendingDisplay(): void {
        this.clearShowTimer();
        this.clearHideTimer();
    }

    private clearShowTimer(): void {
        if (this.showTimeoutId !== null) {
            window.clearTimeout(this.showTimeoutId);
            this.showTimeoutId = null;
        }
    }

    private clearHideTimer(): void {
        if (this.hideTimeoutId !== null) {
            window.clearTimeout(this.hideTimeoutId);
            this.hideTimeoutId = null;
        }
    }

    private destroyOverlay(): void {
        this.overlayRef?.dispose();
        this.overlayRef = null;
    }
}
