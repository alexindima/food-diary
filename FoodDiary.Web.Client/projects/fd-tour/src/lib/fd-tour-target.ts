/** Resolve a selector list to a visible target, including responsive fallbacks. */
export function findTourTarget(document: Document, selector: string): Element | null {
    try {
        return [...document.querySelectorAll(selector)].find(element => isVisibleTarget(document, element)) ?? null;
    } catch {
        return null;
    }
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
