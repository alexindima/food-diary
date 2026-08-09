import { inject, Service } from '@angular/core';
import type { Observable } from 'rxjs';

import { AdminAchievementsService } from '../api/admin-achievements.service';
import type {
    AdminAchievementDefinition,
    CreateAdminAchievementDefinitionRequest,
    UpdateAdminAchievementDefinitionRequest,
} from '../models/admin-achievement.data';

@Service()
export class AdminAchievementsFacade {
    private readonly api = inject(AdminAchievementsService);
    public getAll(): Observable<AdminAchievementDefinition[]> {
        return this.api.getAll();
    }
    public create(request: CreateAdminAchievementDefinitionRequest): Observable<AdminAchievementDefinition> {
        return this.api.create(request);
    }
    public update(id: string, request: UpdateAdminAchievementDefinitionRequest): Observable<AdminAchievementDefinition> {
        return this.api.update(id, request);
    }
}
