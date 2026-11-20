import { inject, Service } from '@angular/core';
import type { Observable } from 'rxjs';

import type { AdminId } from '../../../shared/models/semantics/admin-meaning';
import { AdminRecipeModerationService } from '../api/admin-recipe-moderation.service';
import type {
    AdminRecipeModerationItem,
    AdminRecipeModerationPage,
    AdminRecipeModerationPolicy,
    AdminRecipeModerationRules,
    RecipeModerationPolicyId,
    RecipeModerationRevisionId,
    RecipeModerationStatus,
} from '../models/admin-recipe-moderation.data';

export type AdminRecipeModerationAction = 'approve' | 'requestChanges' | 'retry';

@Service()
export class AdminRecipeModerationFacade {
    private readonly api = inject(AdminRecipeModerationService);

    public list(page: number, limit: number, status?: RecipeModerationStatus): Observable<AdminRecipeModerationPage> {
        return this.api.list(page, limit, status);
    }

    public get(id: RecipeModerationRevisionId): Observable<AdminRecipeModerationItem> {
        return this.api.get(id);
    }

    public getImage(id: RecipeModerationRevisionId, assetId: AdminId<'image-asset'>): Observable<string> {
        return this.api.getImage(id, assetId);
    }

    public applyAction(id: RecipeModerationRevisionId, action: AdminRecipeModerationAction, reason: string): Observable<void> {
        if (action === 'retry') {
            return this.api.retry(id);
        }

        const note = reason.trim();
        return this.api.resolve(id, action === 'approve', note.length > 0 ? note : null);
    }

    public getPolicy(): Observable<AdminRecipeModerationPolicy> {
        return this.api.getPolicy();
    }

    public publishPolicy(rules: AdminRecipeModerationRules): Observable<AdminRecipeModerationPolicy> {
        return this.api.publishPolicy(rules);
    }

    public restorePolicy(id: RecipeModerationPolicyId): Observable<AdminRecipeModerationPolicy> {
        return this.api.restorePolicy(id);
    }
}
