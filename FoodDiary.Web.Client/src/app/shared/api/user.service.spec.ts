import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { afterEach, beforeEach, describe, expect, it } from 'vitest';

import { environment } from '../../../environments/environment';
import { SKIP_GLOBAL_LOADING } from '../../constants/global-loading-context.tokens';
import { type ChangePasswordRequest, UpdateUserDto, type User } from '../models/user.data';
import { UserService } from './user.service';

const BASE_URL = environment.apiUrls.users;
const USER_CALORIES = 2000;
const FASTING_CHECK_IN_REMINDER_HOURS = 12;
const FASTING_CHECK_IN_FOLLOW_UP_REMINDER_HOURS = 20;
const DESIRED_WEIGHT = 75;
const UPDATED_DESIRED_WEIGHT = 70;
const HTTP_BAD_REQUEST = 400;
const HTTP_INTERNAL_SERVER_ERROR = 500;
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
let httpMock: HttpTestingController;

beforeEach(() => {
    TestBed.configureTestingModule({
        providers: [UserService, provideHttpClient(), provideHttpClientTesting()],
    });

    service = TestBed.inject(UserService);
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
        req.flush('Server error', { status: HTTP_INTERNAL_SERVER_ERROR, statusText: 'Internal Server Error' });

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

    it('should clear user signal', () => {
        setCurrentUser();

        service.clearUser();

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
        req.flush('Server error', { status: HTTP_BAD_REQUEST, statusText: 'Bad Request' });
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
        req.flush({ desiredWeight: DESIRED_WEIGHT });
    });

    it('should update desired weight', () => {
        service.updateDesiredWeight(UPDATED_DESIRED_WEIGHT).subscribe(result => {
            expect(result).toBe(UPDATED_DESIRED_WEIGHT);
        });

        const req = httpMock.expectOne(`${BASE_URL}/desired-weight`);
        expect(req.request.method).toBe('PUT');
        expect(req.request.body).toEqual({ desiredWeight: UPDATED_DESIRED_WEIGHT });
        req.flush({ desiredWeight: UPDATED_DESIRED_WEIGHT });
    });
});

function setCurrentUser(user: User = MOCK_USER): void {
    service.getInfo().subscribe();
    const req = httpMock.expectOne(`${BASE_URL}/info`);
    req.flush(user);
    expect(service.user()).toEqual(user);
}
