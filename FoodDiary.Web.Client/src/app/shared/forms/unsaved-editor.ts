/** A routed editor delegates departure to the form that owns its draft. */
export type UnsavedEditor = { canLeaveAsync: () => Promise<boolean> };
