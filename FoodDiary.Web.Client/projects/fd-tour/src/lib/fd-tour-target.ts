/** Resolve a selector list to a visible target, including responsive fallbacks. */
export function findTourTarget(document: Document, selector: string): Element | null {
    try {
        for (const element of document.querySelectorAll(selector)) {
            const target = findRenderedTarget(document, element);
            if (target !== null) {
                return target;
            }
        }
        return null;
    } catch {
        return null;
    }
}

function findRenderedTarget(document: Document, element: Element): Element | null {
    if (!isVisibleTarget(document, element)) {
        return null;
    }
    if (document.defaultView?.getComputedStyle(element).display === 'contents') {
        for (const child of element.children) {
            const target = findRenderedTarget(document, child);
            if (target !== null) {
                return target;
            }
        }
        return null;
    }
    return element;
}

function isVisibleTarget(document: Document, element: Element): boolean {
    for (let ancestor: Element | null = element; ancestor !== null; ancestor = ancestor.parentElement) {
        const style = document.defaultView?.getComputedStyle(ancestor);
        if (
            ancestor.hasAttribute('hidden') ||
            style?.display === 'none' ||
            style?.visibility === 'hidden' ||
            style?.visibility === 'collapse'
        ) {
            return false;
        }
    }
    return true;
}
