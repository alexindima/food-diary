import { HttpClient } from '@angular/common/http';
import { inject, Service } from '@angular/core';
import type { Observable } from 'rxjs';

import { environment } from '../../../../environments/environment';
import type {
    AdminAchievementDefinition,
    CreateAdminAchievementDefinitionRequest,
    UpdateAdminAchievementDefinitionRequest,
} from '../models/admin-achievement.data';

@Service()
export class AdminAchievementsService {
    private readonly http = inject(HttpClient);
    private readonly baseUrl = `${environment.apiUrls.auth.replace(/\/auth$/, '')}/admin/achievement-definitions`;

    public getAll(): Observable<AdminAchievementDefinition[]> {
        return this.http.get<AdminAchievementDefinition[]>(this.baseUrl);
    }

    public create(request: CreateAdminAchievementDefinitionRequest): Observable<AdminAchievementDefinition> {
        return this.http.post<AdminAchievementDefinition>(this.baseUrl, request);
    }

    public update(id: string, request: UpdateAdminAchievementDefinitionRequest): Observable<AdminAchievementDefinition> {
        return this.http.put<AdminAchievementDefinition>(`${this.baseUrl}/${id}`, request);
    }
}
