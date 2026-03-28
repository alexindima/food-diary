import { TestBed } from '@angular/core/testing';
import {
    HTTP_INTERCEPTORS,
    HttpClient,
    HttpContext,
    HttpErrorResponse,
    provideHttpClient,
    withInterceptorsFromDi,
} from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { of, throwError } from 'rxjs';
import { AuthInterceptor } from './auth.interceptor';
import { AuthService } from '../services/auth.service';
import { SKIP_AUTH } from '../constants/http-context.tokens';

describe('AuthInterceptor', () => {
    let http: HttpClient;
    let httpTesting: HttpTestingController;
    let authServiceSpy: jasmine.SpyObj<AuthService>;

    beforeEach(() => {
        authServiceSpy = jasmine.createSpyObj('AuthService', ['getToken', 'refreshToken', 'onLogout']);
        authServiceSpy.onLogout.and.returnValue(Promise.resolve());

        TestBed.configureTestingModule({
            providers: [
                provideHttpClient(withInterceptorsFromDi()),
                provideHttpClientTesting(),
                { provide: HTTP_INTERCEPTORS, useClass: AuthInterceptor, multi: true },
                { provide: AuthService, useValue: authServiceSpy },
            ],
        });

        http = TestBed.inject(HttpClient);
        httpTesting = TestBed.inject(HttpTestingController);
    });

    afterEach(() => {
        httpTesting.verify();
    });

    it('should add Authorization header when token exists', () => {
        authServiceSpy.getToken.and.returnValue('test-token');

        http.get('/api/data').subscribe();

        const req = httpTesting.expectOne('/api/data');
        expect(req.request.headers.get('Authorization')).toBe('Bearer test-token');
        req.flush({});
    });

    it('should not add Authorization header when no token', () => {
        authServiceSpy.getToken.and.returnValue(null);

        http.get('/api/data').subscribe();

        const req = httpTesting.expectOne('/api/data');
        expect(req.request.headers.has('Authorization')).toBeFalse();
        req.flush({});
    });

    it('should skip auth header when SKIP_AUTH context is true', () => {
        authServiceSpy.getToken.and.returnValue('test-token');

        http.get('/api/upload', { context: new HttpContext().set(SKIP_AUTH, true) }).subscribe();

        const req = httpTesting.expectOne('/api/upload');
        expect(req.request.headers.has('Authorization')).toBeFalse();
        req.flush({});
    });

    it('should pass through request on success', () => {
        authServiceSpy.getToken.and.returnValue('test-token');
        const responseData = { id: 1, name: 'Test' };

        http.get<{ id: number; name: string }>('/api/data').subscribe(data => {
            expect(data).toEqual(responseData);
        });

        const req = httpTesting.expectOne('/api/data');
        req.flush(responseData);
    });

    it('should attempt token refresh on 401 error', () => {
        authServiceSpy.getToken.and.returnValue('expired-token');
        authServiceSpy.refreshToken.and.returnValue(of('new-token'));

        http.get('/api/data').subscribe();

        const req = httpTesting.expectOne('/api/data');
        req.flush(null, { status: 401, statusText: 'Unauthorized' });

        expect(authServiceSpy.refreshToken).toHaveBeenCalledTimes(1);

        const retryReq = httpTesting.expectOne('/api/data');
        expect(retryReq.request.headers.get('Authorization')).toBe('Bearer new-token');
        retryReq.flush({});
    });

    it('should retry original request with new token after successful refresh', () => {
        authServiceSpy.getToken.and.returnValue('expired-token');
        authServiceSpy.refreshToken.and.returnValue(of('refreshed-token'));
        const responseData = { success: true };

        http.get<{ success: boolean }>('/api/data').subscribe(data => {
            expect(data).toEqual(responseData);
        });

        const req = httpTesting.expectOne('/api/data');
        req.flush(null, { status: 401, statusText: 'Unauthorized' });

        const retryReq = httpTesting.expectOne('/api/data');
        expect(retryReq.request.headers.get('Authorization')).toBe('Bearer refreshed-token');
        retryReq.flush(responseData);
    });

    it('should logout on refresh failure', () => {
        authServiceSpy.getToken.and.returnValue('expired-token');
        authServiceSpy.refreshToken.and.returnValue(throwError(() => new Error('Refresh failed')));

        http.get('/api/data').subscribe({
            error: (error: Error) => {
                expect(error.message).toBe('Refresh failed');
            },
        });

        const req = httpTesting.expectOne('/api/data');
        req.flush(null, { status: 401, statusText: 'Unauthorized' });

        expect(authServiceSpy.onLogout).toHaveBeenCalledWith(true);
    });

    it('should logout when refresh returns null token', () => {
        authServiceSpy.getToken.and.returnValue('expired-token');
        authServiceSpy.refreshToken.and.returnValue(of(null));

        http.get('/api/data').subscribe({
            error: (error: HttpErrorResponse) => {
                expect(error.status).toBe(401);
            },
        });

        const req = httpTesting.expectOne('/api/data');
        req.flush(null, { status: 401, statusText: 'Unauthorized' });

        expect(authServiceSpy.onLogout).toHaveBeenCalledWith(true);
    });

    it('should not refresh for auth requests (URL contains /auth/)', () => {
        authServiceSpy.getToken.and.returnValue('some-token');

        http.get('/api/auth/login').subscribe({
            error: (error: HttpErrorResponse) => {
                expect(error.status).toBe(401);
            },
        });

        const req = httpTesting.expectOne('/api/auth/login');
        req.flush(null, { status: 401, statusText: 'Unauthorized' });

        expect(authServiceSpy.refreshToken).not.toHaveBeenCalled();
        expect(authServiceSpy.onLogout).not.toHaveBeenCalled();
    });

    it('should propagate non-401 errors without refresh attempt', () => {
        authServiceSpy.getToken.and.returnValue('test-token');

        http.get('/api/data').subscribe({
            error: (error: HttpErrorResponse) => {
                expect(error.status).toBe(500);
            },
        });

        const req = httpTesting.expectOne('/api/data');
        req.flush(null, { status: 500, statusText: 'Internal Server Error' });

        expect(authServiceSpy.refreshToken).not.toHaveBeenCalled();
        expect(authServiceSpy.onLogout).not.toHaveBeenCalled();
    });
});
