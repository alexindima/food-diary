import { onTestFinished, vi } from 'vitest';

export function stubScrollIntoView(): void {
    if (typeof Element.prototype.scrollIntoView === 'function') {
        return;
    }

    Object.defineProperty(Element.prototype, 'scrollIntoView', { configurable: true, value: vi.fn() });
    onTestFinished(() => {
        Reflect.deleteProperty(Element.prototype, 'scrollIntoView');
    });
}
