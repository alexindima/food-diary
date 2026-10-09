import { HttpClient } from '@angular/common/http';
import { inject, Service } from '@angular/core';
import type { Observable } from 'rxjs';
import { map } from 'rxjs';

import { environment } from '../../../../environments/environment';
import { loadPagedCollection } from '../../../shared/api/load-paged-collection';
import { AdminAchievementsSdk } from '../../../shared/api/sdk/generated/api/admin-achievements.service';
import { createSdkConnection } from '../../../shared/api/sdk/sdk-connection';
import type { AdminId } from '../../../shared/models/semantics/admin-meaning';
import type {
    AdminAchievementDefinition,
    CreateAdminAchievementDefinitionRequest,
    UpdateAdminAchievementDefinitionRequest,
} from '../models/admin-achievement.data';
import { adminAchievementDefinitionFromSdk } from './admin-achievements-sdk.mapper';

@Service()
export class AdminAchievementsService {
    private readonly http = inject(HttpClient);
    private readonly baseUrl = `${environment.apiUrls.auth.replace(/\/auth$/, '')}/admin/achievement-definitions`;
    private readonly sdk = createSdkConnection(AdminAchievementsSdk, this.baseUrl, this.http);

    public getAll(): Observable<AdminAchievementDefinition[]> {
        return loadPagedCollection((page, limit) =>
            this.sdk.client
                .getAdminAchievementDefinitions({ version: this.sdk.version, page, limit })
                .pipe(map(items => items.map(adminAchievementDefinitionFromSdk))),
        );
    }

    public create(request: CreateAdminAchievementDefinitionRequest): Observable<AdminAchievementDefinition> {
        return this.sdk.client
            .postAdminAchievementDefinitions({ version: this.sdk.version, createAdminAchievementDefinitionHttpRequest: request })
            .pipe(map(adminAchievementDefinitionFromSdk));
    }

    public update(
        id: AdminId<'achievement-definition'>,
        request: UpdateAdminAchievementDefinitionRequest,
    ): Observable<AdminAchievementDefinition> {
        return this.sdk.client
            .putAdminAchievementDefinitionsById({ version: this.sdk.version, id, updateAdminAchievementDefinitionHttpRequest: request })
            .pipe(map(adminAchievementDefinitionFromSdk));
    }
}
