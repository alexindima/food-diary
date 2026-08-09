import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { afterEach, beforeEach, describe, expect, it } from 'vitest';

import { environment } from '../../../../environments/environment';
import type { CreateAdminAchievementDefinitionRequest, UpdateAdminAchievementDefinitionRequest } from '../models/admin-achievement.data';
import { AdminAchievementsService } from './admin-achievements.service';

const BASE_URL = `${environment.apiUrls.auth.replace(/\/auth$/, '')}/admin/achievement-definitions`;

describe('AdminAchievementsService', () => {
    let service: AdminAchievementsService;
    let http: HttpTestingController;

    beforeEach(() => {
        TestBed.configureTestingModule({
            providers: [AdminAchievementsService, provideHttpClient(), provideHttpClientTesting()],
        });
        service = TestBed.inject(AdminAchievementsService);
        http = TestBed.inject(HttpTestingController);
    });

    afterEach(() => {
        http.verify();
    });

    it('uses separate create and versioned update contracts', () => {
        const create = {
            key: 'meals_20',
            category: 'meals',
            metric: 'TotalMeals' as const,
            threshold: 20,
            titleRu: '20 приёмов',
            titleEn: '20 meals',
            descriptionRu: 'Описание',
            descriptionEn: 'Description',
            icon: 'restaurant',
            sortOrder: 20,
            isActive: true,
        };
        service.create(create).subscribe();
        const createRequest = http.expectOne(BASE_URL);
        const createBody = createRequest.request.body as unknown as CreateAdminAchievementDefinitionRequest;
        expect(createRequest.request.method).toBe('POST');
        expect(createBody.key).toBe('meals_20');
        createRequest.flush({ id: 'definition-id', version: 1, ...create });

        const { key: _key, ...update } = { ...create, version: 1 };
        service.update('definition-id', update).subscribe();
        const updateRequest = http.expectOne(`${BASE_URL}/definition-id`);
        const updateBody = updateRequest.request.body as unknown as UpdateAdminAchievementDefinitionRequest;
        expect(updateRequest.request.method).toBe('PUT');
        expect('key' in updateBody).toBe(false);
        expect(updateBody.version).toBe(1);
        updateRequest.flush({ id: 'definition-id', key: create.key, ...update, version: 2 });
    });
});
