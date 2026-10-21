/** A routed editor delegates departure to the form that owns its draft. */
export type UnsavedEditor = { canLeaveAsync: () => Promise<boolean> };

export function isUnsavedEditor(value: unknown): value is UnsavedEditor {
    return typeof value === 'object' && value !== null && 'canLeaveAsync' in value && typeof value.canLeaveAsync === 'function';
}
