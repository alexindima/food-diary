import { DestroyRef, Directive, ElementRef, ViewContainerRef, effect, inject, input } from '@angular/core';
import { ConnectedPosition, Overlay, OverlayRef } from '@angular/cdk/overlay';
import { TemplatePortal } from '@angular/cdk/portal';
import { Subscription } from 'rxjs';
import { FdUiMenuComponent } from './fd-ui-menu.component';

@Directive({
    selector: '[fdUiMenuTrigger]',
    standalone: true,
    host: {
        '[attr.aria-haspopup]': '"menu"',
        '[attr.aria-expanded]': 'overlayRef?.hasAttached() ? "true" : "false"',
        '(click)': 'handleClick($event)',
        '(keydown)': 'handleKeydown($event)',
    },
})
export class FdUiMenuTriggerDirective {
    private readonly overlay = inject(Overlay);
    private readonly destroyRef = inject(DestroyRef);
    private readonly elementRef = inject(ElementRef<HTMLElement>);
    private readonly viewContainerRef = inject(ViewContainerRef);
    private activeSubscriptions = new Subscription();

    public readonly menu = input<FdUiMenuComponent | null>(null, { alias: 'fdUiMenuTrigger' });

    protected overlayRef: OverlayRef | null = null;

    private readonly positions: ConnectedPosition[] = [
        {
            originX: 'start',
            originY: 'bottom',
            overlayX: 'start',
            overlayY: 'top',
            offsetY: 8,
        },
        {
            originX: 'start',
            originY: 'top',
            overlayX: 'start',
            overlayY: 'bottom',
            offsetY: -8,
        },
    ];

    public constructor() {
        effect(() => {
            const currentMenu = this.menu();
            if (!currentMenu && this.overlayRef?.hasAttached()) {
                this.close();
            }
        });
        this.destroyRef.onDestroy(() => {
            this.activeSubscriptions.unsubscribe();
            this.overlayRef?.dispose();
        });
    }

    protected handleClick(event: MouseEvent): void {
        event.preventDefault();
        this.toggle();
    }

    protected handleKeydown(event: KeyboardEvent): void {
        switch (event.key) {
            case 'ArrowDown':
            case 'Enter':
            case ' ':
                event.preventDefault();
                this.open();
                break;
            case 'ArrowUp':
                event.preventDefault();
                this.open('last');
                break;
            case 'Escape':
                if (this.overlayRef?.hasAttached()) {
                    event.preventDefault();
                    this.close();
                }
                break;
        }
    }

    public toggle(): void {
        if (this.overlayRef?.hasAttached()) {
            this.close();
            return;
        }

        this.open();
    }

    public open(focusTarget: 'first' | 'last' = 'first'): void {
        const menu = this.menu();
        if (!menu) {
            return;
        }

        const overlayRef = this.getOverlayRef();
        if (overlayRef.hasAttached()) {
            return;
        }

        this.activeSubscriptions.unsubscribe();
        this.activeSubscriptions = new Subscription();
        this.activeSubscriptions.add(menu.closed.subscribe(() => this.close()));
        this.activeSubscriptions.add(overlayRef.backdropClick().subscribe(() => this.close()));
        this.activeSubscriptions.add(
            overlayRef.keydownEvents().subscribe(event => {
                if (event.key === 'Escape') {
                    event.preventDefault();
                    this.close();
                    return;
                }

                if (event.key === 'Tab') {
                    this.close(false);
                }
            }),
        );

        overlayRef.updateSize({ minWidth: this.elementRef.nativeElement.getBoundingClientRect().width });
        overlayRef.attach(new TemplatePortal(menu.templateRef, this.viewContainerRef));
        if (focusTarget === 'last') {
            menu.focusLastItem();
            return;
        }

        menu.focusFirstItem();
    }

    public close(restoreFocus = true): void {
        if (!this.overlayRef?.hasAttached()) {
            return;
        }

        this.overlayRef.detach();
        if (restoreFocus) {
            this.elementRef.nativeElement.focus();
        }
    }

    private getOverlayRef(): OverlayRef {
        if (this.overlayRef) {
            return this.overlayRef;
        }

        this.overlayRef = this.overlay.create({
            hasBackdrop: true,
            backdropClass: 'cdk-overlay-transparent-backdrop',
            positionStrategy: this.overlay
                .position()
                .flexibleConnectedTo(this.elementRef)
                .withLockedPosition()
                .withFlexibleDimensions(false)
                .withPush(true)
                .withPositions(this.positions),
            scrollStrategy: this.overlay.scrollStrategies.reposition(),
            panelClass: 'fd-ui-menu-panel',
        });

        return this.overlayRef;
    }
}
