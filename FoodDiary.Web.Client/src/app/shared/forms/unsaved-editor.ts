/** A routed editor delegates departure to the form that owns its draft. */
export type UnsavedEditor = { canLeaveAsync: () => Promise<boolean> };

/** An unmounted form has no draft, so its page can leave immediately. */
export function canLeaveEditor(editor: UnsavedEditor | undefined): true | Promise<boolean> {
    return editor?.canLeaveAsync() ?? true;
}

export function isUnsavedEditor(value: unknown): value is UnsavedEditor {
    return typeof value === 'object' && value !== null && 'canLeaveAsync' in value && typeof value.canLeaveAsync === 'function';
}
