import { HttpErrorResponse, HttpStatusCode, provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { firstValueFrom, type Observable } from 'rxjs';
import { afterEach, beforeEach, describe, expect, it } from 'vitest';

import { environment } from '../../../../environments/environment';
import type { ApiErrorHttpResponse } from '../../../shared/api/sdk/generated/model/api-error-http-response';
import type { CycleHttpResponse } from '../../../shared/api/sdk/generated/model/cycle-http-response';
import type { CycleLogDayHttpResponse } from '../../../shared/api/sdk/generated/model/cycle-log-day-http-response';
import {
    BLEEDING_TYPE_BLEEDING,
    type CreateCyclePayload,
    CYCLE_CONSENT_PURPOSE_FERTILITY_SIGNALS,
    CYCLE_FACTOR_TYPE_HORMONAL_CONTRACEPTION,
    CYCLE_FLOW_MEDIUM,
    CYCLE_SYMPTOM_CATEGORY_PAIN,
    CYCLE_TRACKING_MODE_PERIOD_TRACKING,
    type CycleLogDay,
    type CycleNutritionSummary,
    type CycleResponse,
    MENSTRUAL_EPISODE_STATUS_CONFIRMED,
    OVULATION_TEST_RESULT_NEGATIVE,
    type UpdateCycleSettingsPayload,
    type UpsertCycleDayPayload,
    type UpsertCycleFactorPayload,
} from '../../../shared/models/cycle.data';
import { calendarDate } from '../../../shared/models/semantics/date-value';
import { entityId } from '../../../shared/models/semantics/entity-id';
import { CyclesService } from './cycles.service';

const BASE_URL = environment.apiUrls.cycles;
const MOCK_CYCLE: CycleResponse = {
    id: entityId<'cycle-profile'>('c-1'),
    userId: entityId<'user'>('user-1'),
    mode: CYCLE_TRACKING_MODE_PERIOD_TRACKING,
    goal: 0,
    reproductiveState: 0,
    hideFromDashboard: false,
    confidence: 1,
    trackingStartDate: calendarDate('2026-03-01'),
    averageCycleLength: 28,
    averagePeriodLength: 5,
    lutealLength: 14,
    isRegular: true,
    isOnboardingComplete: true,
    showFertilityEstimates: true,
    discreetNotifications: true,
    notes: null,
    bleedingEntries: [],
    symptoms: [],
    factors: [],
    fertilitySignals: [],
    predictions: null,
};
const MOCK_DAY: CycleLogDay = {
    cycleProfileId: entityId<'cycle-profile'>('c-1'),
    date: calendarDate('2026-03-05'),
    bleedingEntries: [
        {
            id: entityId<'bleeding-entry'>('b-1'),
            cycleProfileId: entityId<'cycle-profile'>('c-1'),
            date: calendarDate('2026-03-05'),
            type: BLEEDING_TYPE_BLEEDING,
            flow: CYCLE_FLOW_MEDIUM,
            painImpact: 2,
            notes: null,
        },
    ],
    symptoms: [],
    fertilitySignal: null,
};
const MOCK_NUTRITION_SUMMARY: CycleNutritionSummary = {
    dateFrom: calendarDate('2026-03-01T00:00:00.000Z'),
    dateTo: calendarDate('2026-03-31T23:59:59.999Z'),
    loggedCycleDays: 4,
    daysWithMeals: 3,
    bleedingDays: 2,
    averageCaloriesOnBleedingDays: 2100,
    averageCaloriesOnNonBleedingCycleDays: 1800,
    averageFiberOnBleedingDays: 18,
    averageFiberOnNonBleedingCycleDays: 28,
    averagePainImpactOnDaysWithMeals: 6,
    hasEnoughNutritionData: true,
};

let service: CyclesService;
let httpMock: HttpTestingController;

beforeEach(() => {
    TestBed.configureTestingModule({
        providers: [CyclesService, provideHttpClient(), provideHttpClientTesting()],
    });

    service = TestBed.inject(CyclesService);
    httpMock = TestBed.inject(HttpTestingController);
});

afterEach(() => {
    httpMock.verify();
});

describe('CyclesService current cycle', () => {
    it('should get current cycle', () => {
        service.getCurrent().subscribe(cycle => {
            expect(cycle).toEqual(MOCK_CYCLE);
        });

        const req = httpMock.expectOne(`${BASE_URL}/current`);
        expect(req.request.method).toBe('GET');
        req.flush(MOCK_CYCLE);
    });

    it('returns null only for a successful empty response', () => {
        service.getCurrent().subscribe(cycle => {
            expect(cycle).toBeNull();
        });

        const req = httpMock.expectOne(`${BASE_URL}/current`);
        req.flush(null, { status: 204, statusText: 'No Content' });
    });

    it('propagates unavailable errors rather than returning an empty profile', () => {
        const unavailableStatus = 503;
        let receivedStatus: number | undefined;
        service.getCurrent().subscribe({
            next: () => {
                throw new Error('A failed request must not emit an empty profile');
            },
            error: (error: unknown) => {
                if (error instanceof HttpErrorResponse) {
                    receivedStatus = error.status;
                }
            },
        });
        const req = httpMock.expectOne(`${BASE_URL}/current`);
        req.flush('Unavailable', { status: unavailableStatus, statusText: 'Service Unavailable' });
        expect(receivedStatus).toBe(unavailableStatus);
    });

    it('should get nutrition summary', () => {
        service
            .getNutritionSummary(calendarDate('2026-03-01T00:00:00.000Z'), calendarDate('2026-03-31T23:59:59.999Z'))
            .subscribe(summary => {
                expect(summary).toEqual(MOCK_NUTRITION_SUMMARY);
            });

        const req = httpMock.expectOne(r => r.url === `${BASE_URL}/current/nutrition-summary` && r.method === 'GET');
        expect(req.request.params.get('dateFrom')).toBe('2026-03-01T00:00:00.000Z');
        expect(req.request.params.get('dateTo')).toBe('2026-03-31T23:59:59.999Z');
        expect(req.request.params.get('timeZoneId')).toBe(new Intl.DateTimeFormat().resolvedOptions().timeZone);
        req.flush(MOCK_NUTRITION_SUMMARY);
    });
});

describe('CyclesService SDK history', () => {
    it('loads persisted cycle history and preserves calendar days separately from revision instants', () => {
        const wire = richCycleResponse();
        const received: Array<CycleResponse | null> = [];
        service.getCurrent().subscribe(cycle => received.push(cycle));
        httpMock.expectOne({ method: 'GET', url: `${BASE_URL}/current` }).flush(wire);

        expect(received).toHaveLength(1);
        expect(received[0]).toMatchObject({
            factors: wire.factors,
            menstrualEpisodes: wire.menstrualEpisodes,
            dayNotes: wire.dayNotes,
            predictionRevisions: wire.predictionRevisions,
            predictions: {
                ...wire.predictions,
                dataSufficiency: undefined,
                patternConsistency: undefined,
                reasonCodes: undefined,
                algorithmVersion: undefined,
            },
        });
    });

    it('keeps each stored symptom and ovulation result when updating a logged day', () => {
        const date = calendarDate('2026-03-05');
        const symptom = { category: CYCLE_SYMPTOM_CATEGORY_PAIN, intensity: 3, tags: ['cramps'] } as const;
        const fertility = { ovulationTestResult: OVULATION_TEST_RESULT_NEGATIVE, hadSex: false } as const;
        const payload: UpsertCycleDayPayload = {
            date,
            notes: 'Afternoon cramps',
            symptoms: [{ ...symptom, tags: [...symptom.tags], clearNote: false }],
            fertilitySignal: { ...fertility, clearNotes: false },
        };
        const wire: CycleLogDayHttpResponse = {
            ...MOCK_DAY,
            date,
            notes: 'Afternoon cramps',
            symptoms: [{ id: 'symptom', cycleProfileId: 'c-1', date, ...symptom, tags: [...symptom.tags] }],
            fertilitySignal: { id: 'test', cycleProfileId: 'c-1', date, ...fertility },
        };
        const results: CycleLogDay[] = [];
        service.upsertDay(MOCK_CYCLE.id, payload).subscribe(day => results.push(day));
        const request = httpMock.expectOne({ method: 'PUT', url: `${BASE_URL}/c-1/days` });
        expect(request.request.body).toEqual(payload);
        request.flush(wire);

        expect(results).toEqual([wire]);
    });

    it('writes a consent withdrawal and keeps the returned audit record', () => {
        const purpose = CYCLE_CONSENT_PURPOSE_FERTILITY_SIGNALS;
        const record = {
            id: 'consent',
            purpose,
            isActive: false,
            grantedAtUtc: '2026-03-05T10:15:00Z',
            revokedAtUtc: '2026-03-05T12:20:00+02:00',
        };
        const wire: CycleHttpResponse = { ...MOCK_CYCLE, consents: [record] };
        const results: CycleResponse[] = [];
        service.updateConsent(MOCK_CYCLE.id, purpose, { granted: false }).subscribe(cycle => results.push(cycle));
        const request = httpMock.expectOne({ method: 'PUT', url: `${BASE_URL}/c-1/consents/${String(purpose)}` });
        expect(request.request.body).toEqual({ granted: false });
        request.flush(wire);

        expect(results.map(cycle => cycle.consents)).toEqual([[record]]);
    });
});

function richCycleResponse(): CycleHttpResponse {
    const recordedDay = '2026-03-05';
    const predictionStart = '2026-04-03';
    const predictionEnd = '2026-04-05';
    return {
        ...MOCK_CYCLE,
        factors: [{ id: 'factor', cycleProfileId: 'c-1', type: CYCLE_FACTOR_TYPE_HORMONAL_CONTRACEPTION, startDate: recordedDay }],
        menstrualEpisodes: [
            {
                id: 'episode',
                cycleProfileId: 'c-1',
                startDate: recordedDay,
                endDate: '2026-03-09',
                status: MENSTRUAL_EPISODE_STATUS_CONFIRMED,
                excludedFromPredictions: false,
            },
        ],
        dayNotes: [{ date: recordedDay, notes: 'Travel' }],
        predictions: {
            confidence: 'medium',
            rationale: 'Observed history',
            nextPeriodStartFrom: predictionStart,
            nextPeriodStartTo: predictionEnd,
            ovulationFrom: null,
        },
        predictionRevisions: [
            {
                id: 'revision',
                generatedAtUtc: '2026-03-12T23:30:00-04:00',
                confidence: 'medium',
                dataSufficiency: 'sufficient',
                patternConsistency: 'stable',
                completedCycleCount: 4,
                calibrationSampleCount: 4,
                reasonCodes: ['stable_cycle_length'],
                algorithmVersion: 'v1',
                nextPeriodStartFrom: predictionStart,
                nextPeriodStartTo: predictionEnd,
            },
        ],
    };
}

describe('CyclesService mutations', () => {
    it('should create cycle', () => {
        const payload: CreateCyclePayload = {
            trackingStartDate: calendarDate('2026-03-01'),
            mode: CYCLE_TRACKING_MODE_PERIOD_TRACKING,
            averageCycleLength: 28,
            averagePeriodLength: 5,
            lutealLength: 14,
            isRegular: true,
            isOnboardingComplete: true,
            showFertilityEstimates: true,
            discreetNotifications: true,
            notes: null,
        };

        service.create(payload).subscribe(cycle => {
            expect(cycle).toEqual(MOCK_CYCLE);
        });

        const req = httpMock.expectOne(BASE_URL);
        expect(req.request.method).toBe('POST');
        expect(req.request.body).toEqual(payload);
        req.flush(MOCK_CYCLE);
    });

    it('should update cycle settings', () => {
        const payload: UpdateCycleSettingsPayload = {
            mode: CYCLE_TRACKING_MODE_PERIOD_TRACKING,
            averageCycleLength: 30,
            averagePeriodLength: 6,
            lutealLength: 13,
            isRegular: false,
            showFertilityEstimates: false,
            discreetNotifications: true,
        };

        service.updateSettings(entityId<'cycle-profile'>('c-1'), payload).subscribe(cycle => {
            expect(cycle).toEqual(MOCK_CYCLE);
        });

        const req = httpMock.expectOne(`${BASE_URL}/c-1/settings`);
        expect(req.request.method).toBe('PUT');
        expect(req.request.body).toEqual(payload);
        req.flush(MOCK_CYCLE);
    });

    it('should delete the cycle profile', () => {
        service.deleteCycle(entityId<'cycle-profile'>('c-1')).subscribe(result => {
            expect(result).toBeNull();
        });

        const req = httpMock.expectOne(`${BASE_URL}/c-1`);
        expect(req.request.method).toBe('DELETE');
        req.flush(null);
    });

    it('should upsert cycle day', () => {
        const payload: UpsertCycleDayPayload = {
            date: calendarDate('2026-03-05'),
            bleeding: {
                type: BLEEDING_TYPE_BLEEDING,
                flow: CYCLE_FLOW_MEDIUM,
                painImpact: 2,
                notes: null,
                clearNotes: false,
            },
            symptoms: [],
            fertilitySignal: null,
        };

        service.upsertDay(entityId<'cycle-profile'>('c-1'), payload).subscribe(day => {
            expect(day).toEqual(MOCK_DAY);
        });

        const req = httpMock.expectOne(`${BASE_URL}/c-1/days`);
        expect(req.request.method).toBe('PUT');
        expect(req.request.body).toEqual(payload);
        req.flush(MOCK_DAY);
    });

    it('should clear cycle day', () => {
        service.clearDay(entityId<'cycle-profile'>('c-1'), calendarDate('2026-03-05T00:00:00.000Z')).subscribe(result => {
            expect(result).toBeNull();
        });

        const req = httpMock.expectOne(r => r.url === `${BASE_URL}/c-1/days` && r.method === 'DELETE');
        expect(req.request.params.get('date')).toBe('2026-03-05T00:00:00.000Z');
        req.flush(null);
    });
});

describe('CyclesService menstrual episodes', () => {
    it('should confirm a period start', () => {
        service.confirmPeriodStart(entityId<'cycle-profile'>('c-1'), calendarDate('2026-03-05T00:00:00.000Z')).subscribe(cycle => {
            expect(cycle).toEqual(MOCK_CYCLE);
        });

        const req = httpMock.expectOne(`${BASE_URL}/c-1/period-start`);
        expect(req.request.method).toBe('PUT');
        expect(req.request.body).toEqual({ date: '2026-03-05T00:00:00.000Z' });
        req.flush(MOCK_CYCLE);
    });

    it('should update menstrual episode dates', () => {
        const payload = {
            startDate: calendarDate('2026-03-04T00:00:00.000Z'),
            endDate: calendarDate('2026-03-08T23:59:59.999Z'),
        };

        service
            .updateMenstrualEpisode(entityId<'cycle-profile'>('c-1'), entityId<'menstrual-episode'>('episode-1'), payload)
            .subscribe(cycle => {
                expect(cycle).toEqual(MOCK_CYCLE);
            });

        const req = httpMock.expectOne(`${BASE_URL}/c-1/menstrual-episodes/episode-1`);
        expect(req.request.method).toBe('PUT');
        expect(req.request.body).toEqual(payload);
        req.flush(MOCK_CYCLE);
    });

    it('should delete a confirmed menstrual episode', () => {
        service.deleteMenstrualEpisode(entityId<'cycle-profile'>('c-1'), entityId<'menstrual-episode'>('episode-1')).subscribe(cycle => {
            expect(cycle).toEqual(MOCK_CYCLE);
        });

        const req = httpMock.expectOne(`${BASE_URL}/c-1/menstrual-episodes/episode-1`);
        expect(req.request.method).toBe('DELETE');
        req.flush(MOCK_CYCLE);
    });
});

describe('CyclesService factor mutations', () => {
    it('should upsert cycle factor', () => {
        const payload: UpsertCycleFactorPayload = {
            factorId: entityId<'cycle-factor'>('factor-1'),
            type: CYCLE_FACTOR_TYPE_HORMONAL_CONTRACEPTION,
            startDate: calendarDate('2026-03-01'),
            endDate: null,
            notes: 'pill',
            clearNotes: false,
        };

        service.upsertFactor(entityId<'cycle-profile'>('c-1'), payload).subscribe(cycle => {
            expect(cycle).toEqual(MOCK_CYCLE);
        });

        const req = httpMock.expectOne(`${BASE_URL}/c-1/factors`);
        expect(req.request.method).toBe('PUT');
        expect(req.request.body).toEqual(payload);
        req.flush(MOCK_CYCLE);
    });
});

const SAVED_PROFILE_ID = entityId<'cycle-profile'>('f5e76483-b157-4885-b307-1a5727ba742f');
const RECORDED_EPISODE_ID = entityId<'menstrual-episode'>('66b83b88-929f-4f0c-ae54-466ceef89f05');
const SAVED_CYCLE_DATE = calendarDate('2026-03-08');
const SAVED_PROFILE_URL = `${BASE_URL}/${SAVED_PROFILE_ID}`;
const SETTINGS_AFTER_EDIT = {
    mode: CYCLE_TRACKING_MODE_PERIOD_TRACKING,
    averageCycleLength: 30,
    averagePeriodLength: 5,
    lutealLength: 14,
    isRegular: false,
    showFertilityEstimates: false,
    discreetNotifications: true,
    hideFromDashboard: true,
} satisfies UpdateCycleSettingsPayload;

describe('Cycle profile unavailable between reading and editing', () => {
    it.each([
        {
            action: 'save settings',
            method: 'PUT',
            path: '/settings',
            body: SETTINGS_AFTER_EDIT,
            dateParam: null,
            perform: (): Observable<unknown> => service.updateSettings(SAVED_PROFILE_ID, SETTINGS_AFTER_EDIT),
        },
        {
            action: 'save consent',
            method: 'PUT',
            path: `/consents/${String(CYCLE_CONSENT_PURPOSE_FERTILITY_SIGNALS)}`,
            body: { granted: false },
            dateParam: null,
            perform: (): Observable<unknown> =>
                service.updateConsent(SAVED_PROFILE_ID, CYCLE_CONSENT_PURPOSE_FERTILITY_SIGNALS, { granted: false }),
        },
        {
            action: 'erase profile',
            method: 'DELETE',
            path: '',
            body: null,
            dateParam: null,
            perform: (): Observable<unknown> => service.deleteCycle(SAVED_PROFILE_ID),
        },
        {
            action: 'clear a day',
            method: 'DELETE',
            path: '/days',
            body: null,
            dateParam: SAVED_CYCLE_DATE,
            perform: (): Observable<unknown> => service.clearDay(SAVED_PROFILE_ID, SAVED_CYCLE_DATE),
        },
    ])('propagates the missing profile while trying to $action', async ({ method, path, body, dateParam, perform }) => {
        const pending = firstValueFrom(perform());
        const response = { error: 'Cycle.NotFound' } satisfies ApiErrorHttpResponse;
        const request = httpMock.expectOne(candidate => candidate.method === method && candidate.url === `${SAVED_PROFILE_URL}${path}`);
        expect(request.request.body).toEqual(body);
        expect(request.request.params.get('date')).toBe(dateParam);
        request.flush(response, { status: HttpStatusCode.NotFound, statusText: 'Not Found' });

        await expect(pending).rejects.toMatchObject({ status: HttpStatusCode.NotFound, error: response });
    });
});

describe('Cycle consent and calendar validation responses', () => {
    it('propagates the tracking-consent validation failure before a profile exists', async () => {
        const payload = {
            trackingStartDate: SAVED_CYCLE_DATE,
            mode: CYCLE_TRACKING_MODE_PERIOD_TRACKING,
            isRegular: true,
            isOnboardingComplete: false,
            showFertilityEstimates: false,
            discreetNotifications: true,
            cycleTrackingConsentGranted: false,
        } satisfies CreateCyclePayload;
        const pending = firstValueFrom(service.create(payload));
        const validation = {
            error: 'Validation.Invalid',
            errors: { cycleTrackingConsentGranted: ['Explicit consent is required to enable cycle tracking.'] },
        } satisfies ApiErrorHttpResponse;
        const request = httpMock.expectOne({ method: 'POST', url: BASE_URL });
        expect(request.request.body).toEqual(payload);
        request.flush(validation, { status: HttpStatusCode.BadRequest, statusText: 'Bad Request' });

        await expect(pending).rejects.toMatchObject({ status: HttpStatusCode.BadRequest, error: validation });
    });

    it('exposes validation when consent was withdrawn before fertility data can be saved', async () => {
        const payload = {
            date: SAVED_CYCLE_DATE,
            symptoms: [],
            fertilitySignal: { ovulationTestResult: OVULATION_TEST_RESULT_NEGATIVE, clearNotes: false },
        } satisfies UpsertCycleDayPayload;
        const pending = firstValueFrom(service.upsertDay(SAVED_PROFILE_ID, payload));
        const validation = {
            error: 'Validation.Invalid',
            errors: { fertilitySignal: ['Active fertility consent is required.'] },
        } satisfies ApiErrorHttpResponse;
        const request = httpMock.expectOne({ method: 'PUT', url: `${SAVED_PROFILE_URL}/days` });
        expect(request.request.body).toEqual(payload);
        request.flush(validation, { status: HttpStatusCode.BadRequest, statusText: 'Bad Request' });

        await expect(pending).rejects.toMatchObject({ status: HttpStatusCode.BadRequest, error: validation });
    });

    it('returns period date validation when the chosen day overlaps a confirmed episode', async () => {
        const pending = firstValueFrom(service.confirmPeriodStart(SAVED_PROFILE_ID, SAVED_CYCLE_DATE));
        const validation = {
            error: 'Validation.Invalid',
            errors: { date: ['Confirmed menstrual episodes cannot overlap.'] },
        } satisfies ApiErrorHttpResponse;
        const request = httpMock.expectOne({ method: 'PUT', url: `${SAVED_PROFILE_URL}/period-start` });
        expect(request.request.body).toEqual({ date: SAVED_CYCLE_DATE });
        request.flush(validation, { status: HttpStatusCode.BadRequest, statusText: 'Bad Request' });

        await expect(pending).rejects.toMatchObject({ status: HttpStatusCode.BadRequest, error: validation });
    });
});

describe('Cycle episode and factor conflicts', () => {
    const episodeEdit = { startDate: SAVED_CYCLE_DATE, endDate: calendarDate('2026-03-12'), excludedFromPredictions: true };

    it.each([
        {
            action: 'update',
            method: 'PUT',
            body: episodeEdit,
            perform: (): Observable<CycleResponse> => service.updateMenstrualEpisode(SAVED_PROFILE_ID, RECORDED_EPISODE_ID, episodeEdit),
        },
        {
            action: 'delete',
            method: 'DELETE',
            body: null,
            perform: (): Observable<CycleResponse> => service.deleteMenstrualEpisode(SAVED_PROFILE_ID, RECORDED_EPISODE_ID),
        },
    ])('propagates validation after a concurrent removal when trying to $action an episode', async ({ method, body, perform }) => {
        const pending = firstValueFrom(perform());
        const response = {
            error: 'Validation.Invalid',
            errors: { menstrualEpisodeId: ['Menstrual episode was not found for this cycle profile.'] },
        } satisfies ApiErrorHttpResponse;
        const request = httpMock.expectOne({ method, url: `${SAVED_PROFILE_URL}/menstrual-episodes/${RECORDED_EPISODE_ID}` });
        expect(request.request.body).toEqual(body);
        request.flush(response, { status: HttpStatusCode.BadRequest, statusText: 'Bad Request' });

        await expect(pending).rejects.toMatchObject({ status: HttpStatusCode.BadRequest, error: response });
    });

    it('propagates duplicate factor date and type for the already selected factor', async () => {
        const payload = {
            factorId: entityId<'cycle-factor'>('1c4561a7-d3eb-4eb2-acde-81eca7213e73'),
            type: CYCLE_FACTOR_TYPE_HORMONAL_CONTRACEPTION,
            startDate: SAVED_CYCLE_DATE,
            endDate: null,
            clearNotes: false,
        } satisfies UpsertCycleFactorPayload;
        const pending = firstValueFrom(service.upsertFactor(SAVED_PROFILE_ID, payload));
        const conflict = { error: 'Cycle.FactorIdentityConflict' } satisfies ApiErrorHttpResponse;
        const request = httpMock.expectOne({ method: 'PUT', url: `${SAVED_PROFILE_URL}/factors` });
        expect(request.request.body).toEqual(payload);
        request.flush(conflict, { status: HttpStatusCode.Conflict, statusText: 'Conflict' });

        await expect(pending).rejects.toMatchObject({ status: HttpStatusCode.Conflict, error: conflict });
    });
});

describe('Cycle nutrition summary is optional', () => {
    it('permits the rest of the cycle report after nutrition fails and recovers for the same date range', async () => {
        const { dateFrom, dateTo } = MOCK_NUTRITION_SUMMARY;
        const matches = (candidate: { url: string; method: string }): boolean =>
            candidate.method === 'GET' && candidate.url === `${BASE_URL}/current/nutrition-summary`;
        const failed = firstValueFrom(service.getNutritionSummary(dateFrom, dateTo));
        httpMock.expectOne(matches).flush(null, { status: HttpStatusCode.ServiceUnavailable, statusText: 'Service Unavailable' });
        await expect(failed).resolves.toBeNull();

        const pending = firstValueFrom(service.getNutritionSummary(dateFrom, dateTo));
        const request = httpMock.expectOne(matches);
        expect(request.request.params.get('dateFrom')).toBe(dateFrom);
        expect(request.request.params.get('dateTo')).toBe(dateTo);
        expect(request.request.params.get('timeZoneId')).toBe(new Intl.DateTimeFormat().resolvedOptions().timeZone);
        request.flush(MOCK_NUTRITION_SUMMARY);
        await expect(pending).resolves.toEqual(MOCK_NUTRITION_SUMMARY);
    });
});
