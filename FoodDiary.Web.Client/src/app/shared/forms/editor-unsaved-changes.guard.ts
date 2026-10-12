import type { CanDeactivateFn } from '@angular/router';

import type { UnsavedEditor } from './unsaved-editor';

export const editorUnsavedChangesGuard: CanDeactivateFn<UnsavedEditor> = async component => component.canLeaveAsync();
