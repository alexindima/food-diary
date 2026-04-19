import { Directive, DestroyRef, ElementRef, Renderer2, inject, input } from '@angular/core';

@Directive({
    selector: '[fdCardHover]',
    host: {
        class: 'fd-card-hover',
        '(mouseenter)': 'onMouseEnter()',
        '(mouseleave)': 'onMouseLeave()',
        '(focusin)': 'onFocusIn()',
        '(focusout)': 'onFocusOut()',
    },
})
export class FdCardHoverDirective {
    public readonly fdCardHoverShadow = input<string | null>(null);
    public readonly fdCardHoverTransform = input<string | null>(null);

    private originalTransform: string | null = null;
    private originalBoxShadow: string | null = null;
    private originalCursor: string | null = null;
    private originalTransition: string | null = null;

    private readonly elementRef = inject(ElementRef<HTMLElement>);
    private readonly renderer = inject(Renderer2);
    private readonly destroyRef = inject(DestroyRef);

    public constructor() {
        const element = this.elementRef.nativeElement;
        const { style } = element;
        this.originalTransform = style.transform || null;
        this.originalBoxShadow = style.boxShadow || null;
        this.originalCursor = style.cursor || null;
        this.originalTransition = style.transition || null;

        this.renderer.setStyle(element, 'cursor', 'pointer');
        this.renderer.setStyle(element, 'transition', 'transform 0.2s ease, box-shadow 0.2s ease');
        this.destroyRef.onDestroy(() => this.cleanupStyles());
    }

    public onMouseEnter(): void {
        this.applyHoverStyles();
    }

    public onMouseLeave(): void {
        this.clearHoverStyles();
    }

    public onFocusIn(): void {
        this.applyHoverStyles();
    }

    public onFocusOut(): void {
        this.clearHoverStyles();
    }

    private applyHoverStyles(): void {
        const element = this.elementRef.nativeElement;
        const styles = getComputedStyle(element);
        const cssTransform = styles.getPropertyValue('--fd-card-hover-transform').trim();
        const cssShadow = styles.getPropertyValue('--fd-card-hover-shadow').trim();
        const transform = this.fdCardHoverTransform() ?? (cssTransform || 'translateY(-2px)');
        const boxShadow =
            this.fdCardHoverShadow() ?? (cssShadow || '0 12px 24px color-mix(in srgb, var(--fd-color-slate-900) 15%, transparent)');
        this.renderer.setStyle(element, 'transform', transform);
        this.renderer.setStyle(element, 'box-shadow', boxShadow);
    }

    private clearHoverStyles(): void {
        this.restoreStyle('transform', this.originalTransform);
        this.restoreStyle('box-shadow', this.originalBoxShadow);
    }

    private cleanupStyles(): void {
        this.restoreStyle('cursor', this.originalCursor);
        this.restoreStyle('transition', this.originalTransition);
        this.restoreStyle('transform', this.originalTransform);
        this.restoreStyle('box-shadow', this.originalBoxShadow);
    }

    private restoreStyle(property: string, value: string | null): void {
        const element = this.elementRef.nativeElement;
        if (value && value.length > 0) {
            this.renderer.setStyle(element, property, value);
        } else {
            this.renderer.removeStyle(element, property);
        }
    }
}
