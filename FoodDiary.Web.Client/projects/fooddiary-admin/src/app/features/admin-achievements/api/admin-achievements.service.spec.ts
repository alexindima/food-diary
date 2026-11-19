import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { firstValueFrom } from 'rxjs';
import { afterEach, beforeEach, describe, expect, it } from 'vitest';

import { environment } from '../../../../environments/environment';
import { COLLECTION_PAGE_SIZE } from '../../../shared/api/load-paged-collection';
import { adminId } from '../../../shared/models/semantics/admin-meaning';
import type { CreateAdminAchievementDefinitionRequest, UpdateAdminAchievementDefinitionRequest } from '../models/admin-achievement.data';
import { AdminAchievementsService } from './admin-achievements.service';

const BASE_URL = `${environment.apiUrls.auth.replace(/\/auth$/, '')}/admin/achievement-definitions`;
const create: CreateAdminAchievementDefinitionRequest = {
    key: 'meals_20',
    category: 'meals',
    metric: 'TotalMeals',
    threshold: 20,
    titleRu: '20 приёмов',
    titleEn: '20 meals',
    descriptionRu: 'Описание',
    descriptionEn: 'Description',
    icon: 'restaurant',
    sortOrder: 20,
    isActive: true,
};

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
        service.create(create).subscribe();
        const createRequest = http.expectOne(BASE_URL);
        const createBody = createRequest.request.body as unknown as CreateAdminAchievementDefinitionRequest;
        expect(createRequest.request.method).toBe('POST');
        expect(createBody.key).toBe('meals_20');
        createRequest.flush({ id: 'definition-id', version: 1, ...create });

        const { key: _key, ...update } = { ...create, version: 1 };
        service.update(adminId<'achievement-definition'>('definition-id'), update).subscribe();
        const updateRequest = http.expectOne(`${BASE_URL}/definition-id`);
        const updateBody = updateRequest.request.body as unknown as UpdateAdminAchievementDefinitionRequest;
        expect(updateRequest.request.method).toBe('PUT');
        expect('key' in updateBody).toBe(false);
        expect(updateBody.version).toBe(1);
        updateRequest.flush({ id: 'definition-id', key: create.key, ...update, version: 2 });
    });

    it('loads another SDK page when the first is full and preserves versions and item order', async () => {
        const result = firstValueFrom(service.getAll());
        const first = http.expectOne(request => request.url === BASE_URL && request.params.get('page') === '1');
        expect(first.request.method).toBe('GET');
        expect(first.request.params.get('limit')).toBe(String(COLLECTION_PAGE_SIZE));
        const firstPage = Array.from({ length: COLLECTION_PAGE_SIZE }, (_, index) => ({
            ...create,
            id: `achievement-${index}`,
            key: `meals_${index}`,
            version: 1,
            sortOrder: index,
        }));
        first.flush(firstPage);
        const second = http.expectOne(request => request.url === BASE_URL && request.params.get('page') === '2');
        expect(second.request.params.get('limit')).toBe(String(COLLECTION_PAGE_SIZE));
        const last = { ...create, id: 'last-achievement', key: 'last_achievement', version: 2 };
        second.flush([last]);
        expect(await result).toEqual([...firstPage, last]);
    });
});
