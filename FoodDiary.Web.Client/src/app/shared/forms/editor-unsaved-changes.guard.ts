import { inject } from '@angular/core';
import type { CanDeactivateFn } from '@angular/router';

import { SessionEventsService } from '../auth/session-events.service';
import type { UnsavedEditor } from './unsaved-editor';

export const editorUnsavedChangesGuard: CanDeactivateFn<UnsavedEditor | null> = async component =>
    inject(SessionEventsService).isSessionEndNavigation || component === null || component.canLeaveAsync();
