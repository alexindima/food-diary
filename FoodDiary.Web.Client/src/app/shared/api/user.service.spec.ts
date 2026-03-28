import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { UserService } from './user.service';
import { environment } from '../../../environments/environment';

describe('UserService', () => {
    let service: UserService;
    let httpMock: HttpTestingController;

    const baseUrl = environment.apiUrls.users;

    const mockUser = {
        id: 'user-1',
        email: 'test@example.com',
        name: 'Test User',
        calories: 2000,
    };

    beforeEach(() => {
        TestBed.configureTestingModule({
            providers: [
                UserService,
                provideHttpClient(),
                provideHttpClientTesting(),
            ],
        });

        service = TestBed.inject(UserService);
        httpMock = TestBed.inject(HttpTestingController);
    });

    afterEach(() => {
        httpMock.verify();
    });

    it('should get user info and update signal', () => {
        service.getInfo().subscribe(result => {
            expect(result).toEqual(mockUser as any);
        });

        const req = httpMock.expectOne(`${baseUrl}/info`);
        expect(req.request.method).toBe('GET');
        req.flush(mockUser);

        expect(service.user()).toEqual(mockUser as any);
    });

    it('should return null on getInfo error', () => {
        service.getInfo().subscribe(result => {
            expect(result).toBeNull();
        });

        const req = httpMock.expectOne(`${baseUrl}/info`);
        req.flush('Server error', { status: 500, statusText: 'Internal Server Error' });

        expect(service.user()).toBeNull();
    });

    it('should update user and update signal', () => {
        const updateData = { name: 'Updated User' };
        const updatedUser = { ...mockUser, name: 'Updated User' };

        service.update(updateData as any).subscribe(result => {
            expect(result).toEqual(updatedUser as any);
        });

        const req = httpMock.expectOne(`${baseUrl}/info`);
        expect(req.request.method).toBe('PATCH');
        expect(req.request.body).toEqual(updateData);
        req.flush(updatedUser);

        expect(service.user()).toEqual(updatedUser as any);
    });

    it('should clear user signal', () => {
        // First set a user via getInfo
        service.getInfo().subscribe();
        const req = httpMock.expectOne(`${baseUrl}/info`);
        req.flush(mockUser);
        expect(service.user()).toEqual(mockUser as any);

        // Now clear
        service.clearUser();
        expect(service.user()).toBeNull();
    });

    it('should change password', () => {
        const request = { currentPassword: 'old', newPassword: 'new' };

        service.changePassword(request as any).subscribe(result => {
            expect(result).toBe(true);
        });

        const req = httpMock.expectOne(`${baseUrl}/password`);
        expect(req.request.method).toBe('PATCH');
        expect(req.request.body).toEqual(request);
        req.flush(null);
    });

    it('should return false on changePassword error', () => {
        const request = { currentPassword: 'old', newPassword: 'new' };

        service.changePassword(request as any).subscribe(result => {
            expect(result).toBe(false);
        });

        const req = httpMock.expectOne(`${baseUrl}/password`);
        req.flush('Server error', { status: 400, statusText: 'Bad Request' });
    });

    it('should delete current user and clear signal', () => {
        // First set a user
        service.getInfo().subscribe();
        const infoReq = httpMock.expectOne(`${baseUrl}/info`);
        infoReq.flush(mockUser);
        expect(service.user()).toEqual(mockUser as any);

        // Delete
        service.deleteCurrentUser().subscribe(result => {
            expect(result).toBe(true);
        });

        const req = httpMock.expectOne(`${baseUrl}/`);
        expect(req.request.method).toBe('DELETE');
        req.flush(null);

        expect(service.user()).toBeNull();
    });

    it('should get user calories', () => {
        service.getUserCalories().subscribe(result => {
            expect(result).toBe(2000);
        });

        const req = httpMock.expectOne(`${baseUrl}/info`);
        expect(req.request.method).toBe('GET');
        req.flush(mockUser);
    });

    it('should get desired weight', () => {
        service.getDesiredWeight().subscribe(result => {
            expect(result).toBe(75);
        });

        const req = httpMock.expectOne(`${baseUrl}/desired-weight`);
        expect(req.request.method).toBe('GET');
        req.flush({ desiredWeight: 75 });
    });

    it('should update desired weight', () => {
        service.updateDesiredWeight(70).subscribe(result => {
            expect(result).toBe(70);
        });

        const req = httpMock.expectOne(`${baseUrl}/desired-weight`);
        expect(req.request.method).toBe('PUT');
        expect(req.request.body).toEqual({ desiredWeight: 70 });
        req.flush({ desiredWeight: 70 });
    });
});
