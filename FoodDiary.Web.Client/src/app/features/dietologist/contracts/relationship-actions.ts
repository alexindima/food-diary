import { InjectionToken } from '@angular/core';
import type { Observable } from 'rxjs';

import type { DietologistPermissions, DietologistRelationship, InviteDietologistRequest } from '../../../shared/models/dietologist.data';

export type DietologistRelationshipActions = {
    getRelationship: () => Observable<DietologistRelationship | null>;
    invite: (request: InviteDietologistRequest) => Observable<void>;
    updatePermissions: (permissions: DietologistPermissions) => Observable<void>;
    revokeRelationship: () => Observable<void>;
};

export const DIETOLOGIST_RELATIONSHIP_ACTIONS = new InjectionToken<DietologistRelationshipActions>('DietologistRelationshipActions');
