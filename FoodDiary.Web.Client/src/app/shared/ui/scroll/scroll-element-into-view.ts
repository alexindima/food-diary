export function scrollElementIntoView(element: HTMLElement): void {
    const browserWindow = element.ownerDocument.defaultView;
    if (browserWindow === null || typeof element.scrollIntoView !== 'function') {
        return;
    }
    const reducedMotion =
        typeof browserWindow.matchMedia === 'function' && browserWindow.matchMedia('(prefers-reduced-motion: reduce)').matches;
    element.scrollIntoView({ behavior: reducedMotion ? 'instant' : 'smooth', block: 'start' });
}
