import { TestBed } from '@angular/core/testing';
import { of } from 'rxjs';
import { describe, expect, it, vi } from 'vitest';

import type { DietologistRecommendation } from '../../../shared/models/dietologist.data';
import { RecommendationsService } from '../api/recommendations.service';
import { RecommendationsFacade } from './recommendations.facade';

describe('RecommendationsFacade', () => {
    it('delegates recommendation loading to the service', () => {
        const recommendation = createRecommendation();
        const { facade, service } = setupFacade([recommendation]);

        facade.getMyRecommendations().subscribe(result => {
            expect(result).toEqual([recommendation]);
        });

        expect(service.getMyRecommendations).toHaveBeenCalledTimes(1);
    });

    it('delegates mark as read to the service', () => {
        const { facade, service } = setupFacade();

        facade.markAsRead('recommendation-1').subscribe();

        expect(service.markAsRead).toHaveBeenCalledWith('recommendation-1');
    });
});

function setupFacade(recommendations: DietologistRecommendation[] = []): {
    facade: RecommendationsFacade;
    service: {
        getMyRecommendations: ReturnType<typeof vi.fn>;
        markAsRead: ReturnType<typeof vi.fn>;
    };
} {
    const service = {
        getMyRecommendations: vi.fn(() => of(recommendations)),
        markAsRead: vi.fn(() => of(void 0)),
    };

    TestBed.resetTestingModule();
    TestBed.configureTestingModule({
        providers: [RecommendationsFacade, { provide: RecommendationsService, useValue: service }],
    });

    return {
        facade: TestBed.inject(RecommendationsFacade),
        service,
    };
}

function createRecommendation(): DietologistRecommendation {
    return {
        id: 'recommendation-1',
        dietologistUserId: 'dietologist-1',
        dietologistFirstName: 'Ada',
        dietologistLastName: 'Lovelace',
        text: 'Add a protein source to breakfast.',
        isRead: false,
        createdAtUtc: '2026-05-01T10:00:00.000Z',
        readAtUtc: null,
    };
}
