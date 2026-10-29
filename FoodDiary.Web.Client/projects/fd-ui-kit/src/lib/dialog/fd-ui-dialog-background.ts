import { OverlayContainer } from '@angular/cdk/overlay';
import { DOCUMENT, isPlatformBrowser } from '@angular/common';
import { DestroyRef, inject, InjectionToken, PLATFORM_ID } from '@angular/core';

/** Shares background isolation across nested dialogs and restores the original DOM state. */
class DialogBackground {
    private count = 0;
    private readonly previous = new Map<Element, string | null>();
    private observer: MutationObserver | undefined;
    private previousOverscroll: { value: string; priority: string } | undefined;

    public constructor(
        private readonly body: HTMLElement | null,
        private readonly overlays: Element | null,
    ) {}

    public acquire(): () => void {
        this.count++;
        if (this.count === 1 && this.body !== null) {
            const style = this.body.ownerDocument.documentElement.style;
            this.previousOverscroll = {
                value: style.getPropertyValue('overscroll-behavior-y'),
                priority: style.getPropertyPriority('overscroll-behavior-y'),
            };
            style.setProperty('overscroll-behavior-y', 'none');
            this.isolate();
            const Observer = this.body.ownerDocument.defaultView?.MutationObserver;
            if (Observer !== undefined) {
                this.observer = new Observer(() => {
                    this.isolate();
                });
                this.observer.observe(this.body, { childList: true });
            }
        }
        let released = false;
        return () => {
            if (!released) {
                released = true;
                if (--this.count === 0) {
                    this.restore();
                }
            }
        };
    }

    public restore(): void {
        this.observer?.disconnect();
        if (this.previousOverscroll !== undefined && this.body !== null) {
            const style = this.body.ownerDocument.documentElement.style;
            const { value, priority } = this.previousOverscroll;
            if (value.length > 0) {
                style.setProperty('overscroll-behavior-y', value, priority);
            } else {
                style.removeProperty('overscroll-behavior-y');
            }
            this.previousOverscroll = undefined;
        }
        for (const [element, value] of this.previous) {
            if (value === null) {
                element.removeAttribute('inert');
            } else {
                element.setAttribute('inert', value);
            }
        }
        this.previous.clear();
    }

    private isolate(): void {
        for (const element of Array.from(this.body?.children ?? [])) {
            if (element === this.overlays || ['SCRIPT', 'STYLE', 'LINK'].includes(element.tagName) || element.hasAttribute('aria-live')) {
                continue;
            }
            if (!this.previous.has(element)) {
                this.previous.set(element, element.getAttribute('inert'));
                element.setAttribute('inert', '');
            }
        }
    }
}

export const FD_UI_DIALOG_BACKGROUND = new InjectionToken<DialogBackground>('FD_UI_DIALOG_BACKGROUND', {
    providedIn: 'root',
    factory: (): DialogBackground => {
        const ownerDocument = inject(DOCUMENT);
        const body = isPlatformBrowser(inject(PLATFORM_ID)) ? ownerDocument.body : null;
        const background = new DialogBackground(body, body !== null ? inject(OverlayContainer).getContainerElement() : null);
        inject(DestroyRef).onDestroy(() => {
            background.restore();
        });
        return background;
    },
});
