import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { environment } from '../../../../environments/environment';
import { FastingService } from './fasting.service';

describe('FastingService', () => {
    let service: FastingService;
    let httpMock: HttpTestingController;

    const baseUrl = environment.apiUrls.fasting;

    beforeEach(() => {
        TestBed.configureTestingModule({
            providers: [FastingService, provideHttpClient(), provideHttpClientTesting()],
        });

        service = TestBed.inject(FastingService);
        httpMock = TestBed.inject(HttpTestingController);
    });

    afterEach(() => {
        httpMock.verify();
    });

    it('should request fasting overview', () => {
        const overview = {
            currentSession: null,
            stats: {
                totalCompleted: 1,
                currentStreak: 1,
                averageDurationHours: 16,
                completionRateLast30Days: 100,
                checkInRateLast30Days: 50,
                lastCheckInAtUtc: '2026-04-12T10:00:00Z',
                topSymptom: 'headache',
            },
            insights: {
                alerts: [],
                insights: [],
            },
            history: {
                data: [],
                page: 1,
                limit: 10,
                totalPages: 0,
                totalItems: 0,
            },
        };

        service.getOverview().subscribe(result => {
            expect(result).toEqual(overview);
        });

        const req = httpMock.expectOne(`${baseUrl}/overview`);
        expect(req.request.method).toBe('GET');
        req.flush(overview);
    });

    it('should fallback overview on error', () => {
        service.getOverview().subscribe(result => {
            expect(result.currentSession).toBeNull();
            expect(result.stats.totalCompleted).toBe(0);
            expect(result.history.data).toEqual([]);
        });

        const req = httpMock.expectOne(`${baseUrl}/overview`);
        req.flush({ message: 'failed' }, { status: 500, statusText: 'Server Error' });
    });

    it('should request paged fasting history with query params', () => {
        const payload = {
            data: [],
            page: 2,
            limit: 10,
            totalPages: 3,
            totalItems: 21,
        };

        service.getHistory({ from: '2026-04-01T00:00:00.000Z', to: '2026-04-30T23:59:59.000Z', page: 2, limit: 10 }).subscribe(result => {
            expect(result).toEqual(payload);
        });

        const req = httpMock.expectOne(
            request =>
                request.url === `${baseUrl}/history` &&
                request.params.get('from') === '2026-04-01T00:00:00.000Z' &&
                request.params.get('to') === '2026-04-30T23:59:59.000Z' &&
                request.params.get('page') === '2' &&
                request.params.get('limit') === '10',
        );
        expect(req.request.method).toBe('GET');
        req.flush(payload);
    });

    it('should send check-in update to current/check-in', () => {
        const response = {
            id: 'session-1',
            startedAtUtc: '2026-04-12T06:00:00Z',
            endedAtUtc: null,
            initialPlannedDurationHours: 16,
            addedDurationHours: 0,
            plannedDurationHours: 16,
            protocol: 'F16_8',
            planType: 'Intermittent',
            occurrenceKind: 'FastingWindow',
            cyclicFastDays: null,
            cyclicEatDays: null,
            cyclicEatDayFastHours: null,
            cyclicEatDayEatingWindowHours: null,
            cyclicPhaseDayNumber: null,
            cyclicPhaseDayTotal: null,
            isCompleted: false,
            status: 'Active',
            notes: null,
            checkInAtUtc: '2026-04-12T10:00:00Z',
            hungerLevel: 2,
            energyLevel: 4,
            moodLevel: 4,
            symptoms: ['weakness'],
            checkInNotes: 'steady',
            checkIns: [],
        };

        service
            .updateCheckIn({
                hungerLevel: 2,
                energyLevel: 4,
                moodLevel: 4,
                symptoms: ['weakness'],
                checkInNotes: 'steady',
            })
            .subscribe(result => {
                expect(result).toEqual(response);
            });

        const req = httpMock.expectOne(`${baseUrl}/current/check-in`);
        expect(req.request.method).toBe('PUT');
        expect(req.request.body).toEqual({
            hungerLevel: 2,
            energyLevel: 4,
            moodLevel: 4,
            symptoms: ['weakness'],
            checkInNotes: 'steady',
        });
        req.flush(response);
    });
});
