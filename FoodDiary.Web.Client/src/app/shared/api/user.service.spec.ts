import { HttpStatusCode, provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import type { Observable } from 'rxjs';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';

import { environment } from '../../../environments/environment';
import { SKIP_GLOBAL_LOADING } from '../../constants/global-loading-context.tokens';
import { SessionEventsService } from '../auth/session-events.service';
import { type ChangePasswordRequest, UpdateUserAppearanceDto, UpdateUserDto, type User } from '../models/user.data';
import { UserService } from './user.service';

const DESIRED_WAIST = 80;

const BASE_URL = environment.apiUrls.users;
const USER_CALORIES = 2000;
const FASTING_CHECK_IN_REMINDER_HOURS = 12;
const FASTING_CHECK_IN_FOLLOW_UP_REMINDER_HOURS = 20;
const DESIRED_WEIGHT = 75;
const UPDATED_DESIRED_WEIGHT = 70;
const UPDATED_THEME = 'dark';
const UPDATED_UI_STYLE = 'compact';
const MOCK_USER: User = {
    id: 'user-1',
    email: 'test@example.com',
    hasPassword: true,
    username: 'test-user',
    calories: USER_CALORIES,
    pushNotificationsEnabled: true,
    fastingPushNotificationsEnabled: false,
    socialPushNotificationsEnabled: true,
    fastingCheckInReminderHours: FASTING_CHECK_IN_REMINDER_HOURS,
    fastingCheckInFollowUpReminderHours: FASTING_CHECK_IN_FOLLOW_UP_REMINDER_HOURS,
    isActive: true,
    isEmailConfirmed: true,
};

let service: UserService;
let sessionEvents: SessionEventsService;
let httpMock: HttpTestingController;

beforeEach(() => {
    TestBed.configureTestingModule({
        providers: [UserService, provideHttpClient(), provideHttpClientTesting()],
    });

    service = TestBed.inject(UserService);
    sessionEvents = TestBed.inject(SessionEventsService);
    httpMock = TestBed.inject(HttpTestingController);
});

afterEach(() => {
    httpMock.verify();
});

describe('UserService info', () => {
    it('should get user info and update signal', () => {
        service.getInfo().subscribe(result => {
            expect(result).toEqual(MOCK_USER);
        });

        const req = httpMock.expectOne(`${BASE_URL}/info`);
        expect(req.request.method).toBe('GET');
        req.flush(MOCK_USER);

        expect(service.user()).toEqual(MOCK_USER);
    });

    it('should return null on getInfo error', () => {
        service.getInfo().subscribe(result => {
            expect(result).toBeNull();
        });

        const req = httpMock.expectOne(`${BASE_URL}/info`);
        req.flush('Server error', { status: HttpStatusCode.InternalServerError, statusText: 'Internal Server Error' });

        expect(service.user()).toBeNull();
    });

    it('should get user info silently when requested', () => {
        service.getInfoSilently().subscribe(result => {
            expect(result).toEqual(MOCK_USER);
        });

        const req = httpMock.expectOne(`${BASE_URL}/info`);
        expect(req.request.method).toBe('GET');
        expect(req.request.context.get(SKIP_GLOBAL_LOADING)).toBe(true);
        req.flush(MOCK_USER);
    });

    it('should update user and update signal', () => {
        const updateData = new UpdateUserDto({ username: 'updated-user' });
        const updatedUser: User = { ...MOCK_USER, username: 'updated-user' };

        service.update(updateData).subscribe(result => {
            expect(result).toEqual(updatedUser);
        });

        const req = httpMock.expectOne(`${BASE_URL}/info`);
        expect(req.request.method).toBe('PATCH');
        expect(req.request.body).toEqual(updateData);
        req.flush(updatedUser);

        expect(service.user()).toEqual(updatedUser);
    });

    it('should update appearance and update signal', () => {
        const appearance = new UpdateUserAppearanceDto({ theme: UPDATED_THEME, uiStyle: UPDATED_UI_STYLE });
        const updatedUser: User = { ...MOCK_USER, theme: UPDATED_THEME, uiStyle: UPDATED_UI_STYLE };

        service.updateAppearance(appearance).subscribe(result => {
            expect(result).toEqual(updatedUser);
        });

        const req = httpMock.expectOne(`${BASE_URL}/preferences/appearance`);
        expect(req.request.method).toBe('PATCH');
        expect(req.request.body).toEqual(appearance);
        req.flush(updatedUser);

        expect(service.user()).toEqual(updatedUser);
    });

    it('should clear user signal', () => {
        setCurrentUser();

        service.clearUser();

        expect(service.user()).toBeNull();
    });

    it('should clear cached user when session ends', () => {
        setCurrentUser();

        sessionEvents.notifySessionEnded();

        expect(service.user()).toBeNull();
    });

    it('should clear cached user when a new authenticated session starts', () => {
        setCurrentUser();

        sessionEvents.notifyAuthenticated();

        expect(service.user()).toBeNull();
    });
});

describe('UserService password', () => {
    it('should change password', () => {
        const request: ChangePasswordRequest = { currentPassword: 'old', newPassword: 'new' };

        service.changePassword(request).subscribe(result => {
            expect(result).toBe(true);
        });

        const req = httpMock.expectOne(`${BASE_URL}/password`);
        expect(req.request.method).toBe('PATCH');
        expect(req.request.body).toEqual(request);
        req.flush(null);
    });

    it('should return false on changePassword error', () => {
        const request: ChangePasswordRequest = { currentPassword: 'old', newPassword: 'new' };

        service.changePassword(request).subscribe(result => {
            expect(result).toBe(false);
        });

        const req = httpMock.expectOne(`${BASE_URL}/password`);
        req.flush('Server error', { status: HttpStatusCode.BadRequest, statusText: 'Bad Request' });
    });

    it('should set password and update hasPassword in signal', () => {
        setCurrentUser({ ...MOCK_USER, hasPassword: false });

        service.setPassword({ newPassword: 'new-secret' }).subscribe(result => {
            expect(result).toBe(true);
        });

        const req = httpMock.expectOne(`${BASE_URL}/password/set`);
        expect(req.request.method).toBe('PATCH');
        expect(req.request.body).toEqual({ newPassword: 'new-secret' });
        req.flush(null);

        expect(service.user()?.hasPassword).toBe(true);
    });
});

describe('UserService deletion', () => {
    it('should delete current user and clear signal', () => {
        setCurrentUser();

        service.deleteCurrentUser().subscribe(result => {
            expect(result).toBe(true);
        });

        const req = httpMock.expectOne(`${BASE_URL}/`);
        expect(req.request.method).toBe('DELETE');
        req.flush(null);

        expect(service.user()).toBeNull();
    });
});

describe('UserService goals', () => {
    it('should get user calories', () => {
        service.getUserCalories().subscribe(result => {
            expect(result).toBe(USER_CALORIES);
        });

        const req = httpMock.expectOne(`${BASE_URL}/info`);
        expect(req.request.method).toBe('GET');
        req.flush(MOCK_USER);
    });

    it('should get desired weight', () => {
        service.getDesiredWeight().subscribe(result => {
            expect(result).toBe(DESIRED_WEIGHT);
        });

        const req = httpMock.expectOne(`${BASE_URL}/desired-weight`);
        expect(req.request.method).toBe('GET');
        req.flush({ desiredWeightKg: DESIRED_WEIGHT });
    });

    it('should update desired weight', () => {
        service.updateDesiredWeight(UPDATED_DESIRED_WEIGHT).subscribe(result => {
            expect(result).toBe(UPDATED_DESIRED_WEIGHT);
        });

        const req = httpMock.expectOne(`${BASE_URL}/desired-weight`);
        expect(req.request.method).toBe('PUT');
        expect(req.request.body).toEqual({ desiredWeightKg: UPDATED_DESIRED_WEIGHT });
        req.flush({ desiredWeightKg: UPDATED_DESIRED_WEIGHT });
    });
});

function setCurrentUser(user: User = MOCK_USER): void {
    service.getInfo().subscribe();
    const req = httpMock.expectOne(`${BASE_URL}/info`);
    req.flush(user);
    expect(service.user()).toEqual(user);
}

describe('UserService dashboard persistence', () => {
    it('saves both layouts and publishes the server user', () => {
        const layout = { web: ['summary', 'weight'], mobile: ['summary', 'hydration'] };
        const user = { ...MOCK_USER, dashboardLayout: layout };
        const next = vi.fn();
        service.updateDashboardLayout(layout).subscribe(next);
        const req = httpMock.expectOne(`${BASE_URL}/info`);
        expect(req.request.method).toBe('PATCH');
        expect(req.request.body).toEqual({ dashboardLayout: layout });
        req.flush(user);
        expect(next).toHaveBeenCalledWith(user);
        expect(service.user()).toEqual(user);
    });

    it('retains the confirmed user when layout save fails', () => {
        service.getInfo().subscribe();
        httpMock.expectOne(`${BASE_URL}/info`).flush(MOCK_USER);
        const next = vi.fn();
        service.updateDashboardLayout({ web: ['summary'], mobile: ['summary'] }).subscribe(next);
        httpMock.expectOne(`${BASE_URL}/info`).flush('offline', { status: 503, statusText: 'Unavailable' });
        expect(next).toHaveBeenCalledWith(null);
        expect(service.user()).toEqual(MOCK_USER);
    });
});

describe('UserService body goal contracts', () => {
    it.each(['weight', 'waist'] as const)('reads %s goal and history', kind => {
        const waist = kind === 'waist';
        const goal = waist
            ? { desiredWaistCm: 80, startWaistCm: 90, startedAtUtc: '2026-03-01' }
            : { desiredWeightKg: 70, startWeightKg: 80, startedAtUtc: '2026-03-01' };
        const next = vi.fn();
        const request: Observable<unknown> = waist ? service.getWaistGoal() : service.getWeightGoal();
        request.subscribe(next);
        const req = httpMock.expectOne(`${BASE_URL}/desired-${kind}`);
        expect(req.request.method).toBe('GET');
        req.flush(goal);
        expect(next).toHaveBeenCalledWith(goal);
        const historyNext = vi.fn();
        const historyRequest: Observable<unknown> = waist ? service.getWaistGoalHistory() : service.getWeightGoalHistory();
        historyRequest.subscribe(historyNext);
        const historyReq = httpMock.expectOne(`${BASE_URL}/${kind}-goals`);
        expect(historyReq.request.method).toBe('GET');
        historyReq.flush([]);
        expect(historyNext).toHaveBeenCalledWith([]);
    });

    it.each(['weight', 'waist'] as const)('returns empty %s goals/history after read errors', kind => {
        const waist = kind === 'waist';
        const next = vi.fn();
        const request: Observable<unknown> = waist ? service.getWaistGoal() : service.getWeightGoal();
        request.subscribe(next);
        httpMock.expectOne(`${BASE_URL}/desired-${kind}`).flush('offline', { status: 503, statusText: 'Unavailable' });
        expect(next).toHaveBeenCalledWith(
            waist
                ? { desiredWaistCm: null, startWaistCm: null, startedAtUtc: null }
                : { desiredWeightKg: null, startWeightKg: null, startedAtUtc: null },
        );
        const historyNext = vi.fn();
        const historyRequest: Observable<unknown> = waist ? service.getWaistGoalHistory() : service.getWeightGoalHistory();
        historyRequest.subscribe(historyNext);
        httpMock.expectOne(`${BASE_URL}/${kind}-goals`).flush('offline', { status: 503, statusText: 'Unavailable' });
        expect(historyNext).toHaveBeenCalledWith([]);
    });
});

describe('UserService body goal writes', () => {
    it.each(['weight', 'waist'] as const)('saves and clears %s target using canonical units', kind => {
        const waist = kind === 'waist';
        for (const value of [DESIRED_WEIGHT, null]) {
            const next = vi.fn();
            const request: Observable<unknown> = waist ? service.updateWaistGoal(value) : service.updateWeightGoal(value);
            request.subscribe(next);
            const req = httpMock.expectOne(`${BASE_URL}/desired-${kind}`);
            expect(req.request.method).toBe('PUT');
            expect(req.request.body).toEqual(waist ? { desiredWaistCm: value } : { desiredWeightKg: value });
            const result = waist
                ? { desiredWaistCm: value, startWaistCm: null, startedAtUtc: null }
                : { desiredWeightKg: value, startWeightKg: null, startedAtUtc: null };
            req.flush(result);
            expect(next).toHaveBeenCalledWith(result);
        }
    });

    it.each(['weight', 'waist'] as const)('propagates %s write errors', kind => {
        const waist = kind === 'waist';
        const requests: Array<Observable<unknown>> = [
            waist ? service.updateWaistGoal(DESIRED_WEIGHT) : service.updateWeightGoal(DESIRED_WEIGHT),
            waist ? service.updateDesiredWaist(DESIRED_WEIGHT) : service.updateDesiredWeight(DESIRED_WEIGHT),
        ];
        for (const request of requests) {
            const error = vi.fn();
            const next = vi.fn();
            request.subscribe({ error, next });
            httpMock.expectOne(`${BASE_URL}/desired-${kind}`).flush('offline', { status: 503, statusText: 'Unavailable' });
            expect(next).not.toHaveBeenCalled();
            expect(error).toHaveBeenCalledWith(expect.objectContaining({ status: 503 }));
        }
    });

    it('reads and clears the scalar waist target', () => {
        const next = vi.fn();
        service.getDesiredWaist().subscribe(next);
        httpMock.expectOne(`${BASE_URL}/desired-waist`).flush({ desiredWaistCm: 80 });
        expect(next).toHaveBeenLastCalledWith(DESIRED_WAIST);
        service.updateDesiredWaist(null).subscribe(next);
        const req = httpMock.expectOne(`${BASE_URL}/desired-waist`);
        expect(req.request.body).toEqual({ desiredWaistCm: null });
        req.flush({ desiredWaistCm: null });
        expect(next).toHaveBeenLastCalledWith(null);
    });
});

describe('UserService overview state', () => {
    it('loads overview user and clears stale user data after an overview error', () => {
        const overview = { user: MOCK_USER, notificationPreferences: {}, webPushSubscriptions: [], dietologistRelationship: null };
        const next = vi.fn();
        service.getOverview().subscribe(next);
        httpMock.expectOne(`${BASE_URL}/overview`).flush(overview);
        expect(next).toHaveBeenLastCalledWith(overview);
        expect(service.user()).toEqual(MOCK_USER);
        service.getOverview().subscribe(next);
        httpMock.expectOne(`${BASE_URL}/overview`).flush('offline', { status: 503, statusText: 'Unavailable' });
        expect(next).toHaveBeenLastCalledWith(null);
        expect(service.user()).toBeNull();
    });

    it('clears stale user information when a silent fetch fails', () => {
        service.getInfo().subscribe();
        httpMock.expectOne(`${BASE_URL}/info`).flush(MOCK_USER);
        const next = vi.fn();
        service.getInfoSilently().subscribe(next);
        const req = httpMock.expectOne(`${BASE_URL}/info`);
        expect(req.request.context.get(SKIP_GLOBAL_LOADING)).toBe(true);
        req.flush('offline', { status: 503, statusText: 'Unavailable' });
        expect(next).toHaveBeenCalledWith(null);
        expect(service.user()).toBeNull();
    });
});

describe('UserService consent state', () => {
    it.each([false, true])('accepts and revokes consent with cached user=%s', cached => {
        if (cached) {
            service.getInfo().subscribe();
            httpMock.expectOne(`${BASE_URL}/info`).flush(MOCK_USER);
        }
        service.acceptAiConsent().subscribe();
        const accept = httpMock.expectOne(`${BASE_URL}/ai-consent`);
        expect(accept.request.method).toBe('POST');
        expect(accept.request.body).toEqual({});
        accept.flush(null);
        if (cached) {
            expect(service.user()?.aiConsentAcceptedAt).toEqual(expect.any(String));
        } else {
            expect(service.user()).toBeNull();
        }
        service.revokeAiConsent().subscribe();
        const revoke = httpMock.expectOne(`${BASE_URL}/ai-consent`);
        expect(revoke.request.method).toBe('DELETE');
        revoke.flush(null);
        if (cached) {
            expect(service.user()?.aiConsentAcceptedAt).toBeNull();
        } else {
            expect(service.user()).toBeNull();
        }
    });

    it.each(['accept', 'revoke'] as const)('does not change consent when %s fails', action => {
        service.getInfo().subscribe();
        httpMock.expectOne(`${BASE_URL}/info`).flush(MOCK_USER);
        const error = vi.fn();
        const next = vi.fn();
        (action === 'accept' ? service.acceptAiConsent() : service.revokeAiConsent()).subscribe({ next, error });
        httpMock.expectOne(`${BASE_URL}/ai-consent`).flush('offline', { status: 503, statusText: 'Unavailable' });
        expect(next).not.toHaveBeenCalled();
        expect(error).toHaveBeenCalledWith(expect.objectContaining({ status: 503 }));
        expect(service.user()).toEqual(MOCK_USER);
    });
});

describe('UserService failure contracts', () => {
    it.each([
        { method: 'update', endpoint: 'info', fallback: null },
        { method: 'updateAppearance', endpoint: 'preferences/appearance', fallback: null },
        { method: 'deleteCurrentUser', endpoint: '', fallback: false },
        { method: 'setPassword', endpoint: 'password/set', fallback: false },
        { method: 'getDesiredWeight', endpoint: 'desired-weight', fallback: null },
        { method: 'getDesiredWaist', endpoint: 'desired-waist', fallback: null },
    ] as const)('returns the documented fallback for $method and retains confirmed user', ({ method, endpoint, fallback }) => {
        service.getInfo().subscribe();
        httpMock.expectOne(`${BASE_URL}/info`).flush(MOCK_USER);
        let request: Observable<unknown>;
        switch (method) {
            case 'update': {
                request = service.update(new UpdateUserDto({}));
                break;
            }
            case 'updateAppearance': {
                request = service.updateAppearance(new UpdateUserAppearanceDto({}));
                break;
            }
            case 'setPassword': {
                request = service.setPassword({ newPassword: 'test-only-password' });
                break;
            }
            case 'deleteCurrentUser':
            case 'getDesiredWeight':
            case 'getDesiredWaist': {
                request = service[method]();
            }
        }
        const next = vi.fn();
        request.subscribe(next);
        httpMock.expectOne(`${BASE_URL}/${endpoint}`).flush('offline', { status: 503, statusText: 'Unavailable' });
        expect(next).toHaveBeenCalledWith(fallback);
        expect(service.user()).toEqual(MOCK_USER);
    });
});
