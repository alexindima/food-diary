import { Directive, ElementRef, HostListener, ViewContainerRef, input, inject } from '@angular/core';
import { DomSanitizer, SafeHtml } from '@angular/platform-browser';
import { ConnectedPosition, Overlay, OverlayRef } from '@angular/cdk/overlay';
import { ComponentPortal, TemplatePortal } from '@angular/cdk/portal';
import { TemplateRef } from '@angular/core';
import { FdUiHintOverlayComponent } from './fd-ui-hint-overlay.component';

type HintContent = string | TemplateRef<unknown> | null;
type HintPosition = 'top' | 'bottom' | 'left' | 'right';

@Directive({
    selector: '[fdUiHint]',
    standalone: true,
})
export class FdUiHintDirective {
    public readonly fdUiHint = input<HintContent>(null);
    public readonly fdUiHintHtml = input(false);
    public readonly fdUiHintContext = input<Record<string, unknown> | null>(null);
    public readonly fdUiHintShowDelay = input(500);
    public readonly fdUiHintHideDelay = input(0);
    public readonly fdUiHintPosition = input<HintPosition>('top');

    private readonly overlay = inject(Overlay);
    private readonly elementRef = inject(ElementRef<HTMLElement>);
    private readonly viewContainerRef = inject(ViewContainerRef);
    private readonly sanitizer = inject(DomSanitizer);
    private overlayRef: OverlayRef | null = null;
    private showTimeoutId: number | null = null;
    private hideTimeoutId: number | null = null;

    @HostListener('mouseenter')
    public onMouseEnter(): void {
        this.queueShow();
    }

    @HostListener('mouseleave')
    public onMouseLeave(): void {
        this.queueHide();
    }

    @HostListener('focusin')
    public onFocusIn(): void {
        this.queueShow();
    }

    @HostListener('focusout')
    public onFocusOut(): void {
        this.queueHide();
    }

    @HostListener('click')
    public onClick(): void {
        this.hide();
    }

    public ngOnDestroy(): void {
        this.clearTimers();
        this.destroyOverlay();
    }

    private queueShow(): void {
        if (!this.fdUiHint()) {
            return;
        }
        this.clearHideTimer();
        if (this.showTimeoutId !== null) {
            return;
        }
        this.showTimeoutId = window.setTimeout(() => {
            this.showTimeoutId = null;
            this.show();
        }, this.fdUiHintShowDelay());
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
        if (!content) {
            return;
        }

        if (!this.overlayRef) {
            this.overlayRef = this.createOverlay();
        }

        if (this.overlayRef.hasAttached()) {
            return;
        }

        if (content instanceof TemplateRef) {
            const context = this.fdUiHintContext() ?? {};
            this.overlayRef.attach(new TemplatePortal(content, this.viewContainerRef, context));
            return;
        }

        const portal = new ComponentPortal(FdUiHintOverlayComponent, this.viewContainerRef);
        const ref = this.overlayRef.attach(portal);
        if (this.fdUiHintHtml()) {
            ref.setInput('contentHtml', this.toSafeHtml(content));
            ref.setInput('contentText', null);
        } else {
            ref.setInput('contentText', content);
            ref.setInput('contentHtml', null);
        }
    }

    private hide(): void {
        if (!this.overlayRef?.hasAttached()) {
            return;
        }
        this.overlayRef.detach();
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
        const offset = 8;
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

    private toSafeHtml(content: string): SafeHtml {
        return this.sanitizer.bypassSecurityTrustHtml(content);
    }

    private clearTimers(): void {
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
