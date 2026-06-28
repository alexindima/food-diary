import { TestBed } from '@angular/core/testing';
import { ActivatedRoute, convertToParamMap } from '@angular/router';
import { FdTourService } from 'fd-tour';
import { BehaviorSubject, type Observable, of, Subject, throwError } from 'rxjs';
import { describe, expect, it, vi } from 'vitest';

import { provideTranslateTesting } from '../../../../../testing/translate-testing.module';
import type { DietologistRecommendation } from '../../../../shared/models/dietologist.data';
import { LocalizedTourDefinitionService } from '../../../../shared/tours/localized-tour-definition.service';
import { RecommendationsFacade } from '../../lib/recommendations.facade';
import { RecommendationsPageComponent } from './recommendations-page';

describe('RecommendationsPageComponent loading', () => {
    it('loads recommendations and maps names, selected state and unread state', () => {
        const selectedRecommendation = createRecommendation({ id: 'selected-1', isRead: true });
        const genericRecommendation = createRecommendation({
            id: 'generic-1',
            dietologistFirstName: null,
            dietologistLastName: null,
        });
        const { component, facade } = createComponent({
            recommendations: [selectedRecommendation, genericRecommendation],
            recommendationId: selectedRecommendation.id,
        });

        const items = component['recommendationItems']();

        expect(facade.getMyRecommendations).toHaveBeenCalledTimes(1);
        expect(component['isLoading']()).toBe(false);
        expect(component['errorKey']()).toBeNull();
        expect(items).toHaveLength(2);
        expect(items[0]).toMatchObject({
            id: selectedRecommendation.id,
            dietologistName: 'Ada Lovelace',
            isSelected: true,
            isMarkingRead: false,
        });
        expect(items[1]).toMatchObject({
            id: genericRecommendation.id,
            dietologistName: null,
            isSelected: false,
        });
    });

    it('shows load error and clears recommendations when loading fails', () => {
        const { component } = createComponent({ loadError: true });

        expect(component['isLoading']()).toBe(false);
        expect(component['recommendations']()).toEqual([]);
        expect(component['errorKey']()).toBe('RECOMMENDATIONS.LOAD_ERROR');
    });

    it('keeps loading state until the recommendations request resolves', () => {
        const recommendations$ = new Subject<DietologistRecommendation[]>();
        const { component } = createComponent({ recommendations$ });

        expect(component['isLoading']()).toBe(true);

        recommendations$.next([createRecommendation()]);
        recommendations$.complete();

        expect(component['isLoading']()).toBe(false);
        expect(component['recommendationItems']()).toHaveLength(1);
    });
});

describe('RecommendationsPageComponent mark read', () => {
    it('marks the selected recommendation as read after recommendations load', () => {
        const recommendation = createRecommendation({ id: 'recommendation-1', isRead: false });
        const { component, facade } = createComponent({
            recommendations: [recommendation],
            recommendationId: recommendation.id,
        });

        expect(facade.markAsRead).toHaveBeenCalledWith(recommendation.id);
        expect(component['recommendationItems']()[0]).toMatchObject({
            id: recommendation.id,
            isRead: true,
            isMarkingRead: false,
        });
    });

    it('marks a recommendation as read when it is clicked', () => {
        const recommendation = createRecommendation({ id: 'recommendation-1', isRead: false });
        const { component, facade } = createComponent({ recommendations: [recommendation] });

        component['markRecommendationAsRead'](recommendation);

        expect(facade.markAsRead).toHaveBeenCalledWith(recommendation.id);
        expect(component['recommendationItems']()[0].isRead).toBe(true);
    });

    it('does not mark already read recommendations again', () => {
        const recommendation = createRecommendation({ isRead: true });
        const { component, facade } = createComponent({ recommendations: [recommendation] });

        component['markRecommendationAsRead'](recommendation);

        expect(facade.markAsRead).not.toHaveBeenCalled();
    });

    it('keeps only one in-flight mark-read request per recommendation', () => {
        const recommendation = createRecommendation({ isRead: false });
        const markRead$ = new Subject<void>();
        const { component, facade } = createComponent({ recommendations: [recommendation], markRead$ });

        component['markRecommendationAsRead'](recommendation);
        component['markRecommendationAsRead'](recommendation);

        expect(facade.markAsRead).toHaveBeenCalledTimes(1);
        expect(component['recommendationItems']()[0].isMarkingRead).toBe(true);

        markRead$.next();
        markRead$.complete();

        expect(component['recommendationItems']()[0]).toMatchObject({
            isRead: true,
            isMarkingRead: false,
        });
    });

    it('shows mark-read error and clears in-flight state when the request fails', () => {
        const recommendation = createRecommendation({ isRead: false });
        const { component } = createComponent({ recommendations: [recommendation], markReadError: true });

        component['markRecommendationAsRead'](recommendation);

        expect(component['errorKey']()).toBe('RECOMMENDATIONS.MARK_READ_ERROR');
        expect(component['recommendationItems']()[0]).toMatchObject({
            isRead: false,
            isMarkingRead: false,
        });
    });
});

describe('RecommendationsPageComponent route and tour', () => {
    it('marks a recommendation when query params select it after initial load', () => {
        const queryParams$ = new BehaviorSubject(convertToParamMap({}));
        const recommendation = createRecommendation({ id: 'recommendation-1', isRead: false });
        const { facade } = createComponent({ recommendations: [recommendation], queryParams$ });

        queryParams$.next(convertToParamMap({ recommendationId: recommendation.id }));

        expect(facade.markAsRead).toHaveBeenCalledWith(recommendation.id);
    });

    it('starts the localized recommendations tour', () => {
        const tourDefinition = { steps: [] };
        const tourService = { start: vi.fn() };
        const localizedTour = { build: vi.fn(() => tourDefinition) };
        const { component } = createComponent({ tourService, localizedTour });

        component['startRecommendationsTour']();

        expect(localizedTour.build).toHaveBeenCalledTimes(1);
        expect(tourService.start).toHaveBeenCalledWith(tourDefinition, { force: true });
    });
});

type CreateComponentOptions = {
    recommendations?: DietologistRecommendation[];
    recommendations$?: Subject<DietologistRecommendation[]>;
    loadError?: boolean;
    markRead$?: Subject<void>;
    markReadError?: boolean;
    recommendationId?: string;
    queryParams$?: BehaviorSubject<ReturnType<typeof convertToParamMap>>;
    tourService?: { start: ReturnType<typeof vi.fn> };
    localizedTour?: { build: ReturnType<typeof vi.fn> };
};

function createComponent(options: CreateComponentOptions = {}): {
    component: RecommendationsPageComponent;
    facade: {
        getMyRecommendations: ReturnType<typeof vi.fn>;
        markAsRead: ReturnType<typeof vi.fn>;
    };
} {
    const queryParams$ = createQueryParams(options);
    const recommendations$ = createRecommendationsStream(options);
    const markRead$ = createMarkReadStream(options);
    const facade = {
        getMyRecommendations: vi.fn(() => recommendations$),
        markAsRead: vi.fn(() => markRead$),
    };
    const tourService = options.tourService ?? { start: vi.fn() };
    const localizedTour = options.localizedTour ?? { build: vi.fn(() => ({ steps: [] })) };

    TestBed.resetTestingModule();
    TestBed.configureTestingModule({
        providers: [
            provideTranslateTesting(),
            { provide: RecommendationsFacade, useValue: facade },
            { provide: ActivatedRoute, useValue: { queryParamMap: queryParams$.asObservable() } },
            { provide: FdTourService, useValue: tourService },
            { provide: LocalizedTourDefinitionService, useValue: localizedTour },
        ],
    });

    return {
        component: TestBed.runInInjectionContext(() => new RecommendationsPageComponent()),
        facade,
    };
}

function createQueryParams(options: CreateComponentOptions): BehaviorSubject<ReturnType<typeof convertToParamMap>> {
    if (options.queryParams$ !== undefined) {
        return options.queryParams$;
    }

    return new BehaviorSubject(
        convertToParamMap(options.recommendationId === undefined ? {} : { recommendationId: options.recommendationId }),
    );
}

function createRecommendationsStream(options: CreateComponentOptions): Observable<DietologistRecommendation[]> {
    if (options.recommendations$ !== undefined) {
        return options.recommendations$;
    }

    return options.loadError === true ? throwError(() => new Error('load failed')) : of(options.recommendations ?? []);
}

function createMarkReadStream(options: CreateComponentOptions): Observable<void> {
    if (options.markRead$ !== undefined) {
        return options.markRead$;
    }

    return options.markReadError === true ? throwError(() => new Error('mark read failed')) : of(void 0);
}

function createRecommendation(overrides: Partial<DietologistRecommendation> = {}): DietologistRecommendation {
    return {
        id: 'recommendation-1',
        dietologistUserId: 'dietologist-1',
        dietologistFirstName: 'Ada',
        dietologistLastName: 'Lovelace',
        text: 'Add a protein source to breakfast.',
        isRead: false,
        createdAtUtc: '2026-05-01T10:00:00.000Z',
        readAtUtc: null,
        ...overrides,
    };
}
