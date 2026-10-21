import { type ChildrenOutletContexts, PRIMARY_OUTLET } from '@angular/router';

import { isUnsavedEditor, type UnsavedEditor } from '../../shared/forms/unsaved-editor';

export async function confirmRoutedEditorsReloadAsync(contexts: ChildrenOutletContexts): Promise<boolean> {
    const editors: UnsavedEditor[] = [];
    let context = contexts.getContext(PRIMARY_OUTLET);
    while (context !== null) {
        const outlet = context.outlet;
        const page: unknown = outlet?.isActivated === true ? outlet.component : null;
        if (isUnsavedEditor(page)) {
            editors.push(page);
        }
        context = context.children.getContext(PRIMARY_OUTLET);
    }
    // Feature containers can own another router-outlet. Ask the deepest editor first.
    for (const editor of editors.reverse()) {
        if (!(await editor.canLeaveAsync())) {
            return false;
        }
    }
    return true;
}
