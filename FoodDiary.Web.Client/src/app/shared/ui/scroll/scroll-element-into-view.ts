export function scrollElementIntoView(element: HTMLElement): void {
    const browserWindow: Partial<Window> | null = element.ownerDocument.defaultView;
    if (browserWindow === null || typeof element.scrollIntoView !== 'function') {
        return;
    }
    const reducedMotion = browserWindow.matchMedia?.('(prefers-reduced-motion: reduce)').matches ?? false;
    element.scrollIntoView({ behavior: reducedMotion ? 'instant' : 'smooth', block: 'start' });
}
