import { HttpClient } from '@angular/common/http';
import { inject, Service } from '@angular/core';
import { map, type Observable } from 'rxjs';

import { environment } from '../../../../environments/environment';
import { AdminModerationSdk } from '../../../shared/api/sdk/generated/api/admin-moderation.service';
import { createSdkConnection } from '../../../shared/api/sdk/sdk-connection';
import { requireSdkFields } from '../../../shared/api/sdk/sdk-response';
import type { AdminId } from '../../../shared/models/semantics/admin-meaning';
import type {
    AdminRecipeModerationItem,
    AdminRecipeModerationPage,
    AdminRecipeModerationPolicy,
    AdminRecipeModerationRules,
    RecipeModerationPolicyId,
    RecipeModerationRevisionId,
    RecipeModerationStatus,
} from '../models/admin-recipe-moderation.data';
import {
    recipeModerationItemFromSdk,
    recipeModerationPageFromSdk,
    recipeModerationPolicyFromSdk,
} from './admin-recipe-moderation-sdk.mapper';

@Service()
export class AdminRecipeModerationService {
    private readonly sdk = createSdkConnection(AdminModerationSdk, environment.apiUrls.auth, inject(HttpClient));

    public list(page: number, limit: number, status?: RecipeModerationStatus): Observable<AdminRecipeModerationPage> {
        return this.sdk.client
            .getAdminModerationRecipes({ version: this.sdk.version, page, limit, status })
            .pipe(map(recipeModerationPageFromSdk));
    }

    public get(id: RecipeModerationRevisionId): Observable<AdminRecipeModerationItem> {
        return this.sdk.client.getAdminModerationRecipe({ version: this.sdk.version, id }).pipe(map(recipeModerationItemFromSdk));
    }

    public getImage(id: RecipeModerationRevisionId, assetId: AdminId<'image-asset'>): Observable<string> {
        return this.sdk.client
            .getAdminModerationRecipeImage({ version: this.sdk.version, id, assetId })
            .pipe(map(response => requireSdkFields(response, ['dataUrl']).dataUrl));
    }

    public resolve(id: RecipeModerationRevisionId, approve: boolean, reason: string | null): Observable<void> {
        return this.sdk.client.postAdminModerationRecipeResolve({
            version: this.sdk.version,
            id,
            adminRecipeModerationResolveHttpRequest: { approve, reason },
        });
    }

    public retry(id: RecipeModerationRevisionId): Observable<void> {
        return this.sdk.client.postAdminModerationRecipeRetry({ version: this.sdk.version, id });
    }

    public getPolicy(): Observable<AdminRecipeModerationPolicy> {
        return this.sdk.client.getAdminModerationRules({ version: this.sdk.version }).pipe(map(recipeModerationPolicyFromSdk));
    }

    public publishPolicy(rules: AdminRecipeModerationRules): Observable<AdminRecipeModerationPolicy> {
        return this.sdk.client
            .putAdminModerationRules({ version: this.sdk.version, adminRecipeModerationRulesHttpRequest: rules })
            .pipe(map(recipeModerationPolicyFromSdk));
    }

    public restorePolicy(id: RecipeModerationPolicyId): Observable<AdminRecipeModerationPolicy> {
        return this.sdk.client.postAdminModerationRulesRestore({ version: this.sdk.version, id }).pipe(map(recipeModerationPolicyFromSdk));
    }
}
