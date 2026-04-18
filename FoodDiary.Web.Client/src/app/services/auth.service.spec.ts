import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { AuthService } from './auth.service';
import { NavigationService } from './navigation.service';
import { LocalizationService } from './localization.service';
import { QuickMealService } from '../features/meals/lib/quick-meal.service';
import { LoginRequest, RegisterRequest } from '../features/auth/models/auth.data';
import { environment } from '../../environments/environment';

function createFakeJwt(payload: Record<string, unknown>): string {
    const header = btoa(JSON.stringify({ alg: 'HS256', typ: 'JWT' }));
    const body = btoa(JSON.stringify(payload));
    const sig = btoa('signature');
    return `${header}.${body}.${sig}`;
}

describe('AuthService', () => {
    let service: AuthService;
    let httpMock: HttpTestingController;
    let navigationServiceSpy: any;
    let localizationServiceSpy: any;
    let quickMealServiceSpy: any;

    const authBaseUrl = environment.apiUrls.auth;

    beforeEach(() => {
        localStorage.clear();
        sessionStorage.clear();

        navigationServiceSpy = {
            navigateToAuth: vi.fn(),
            navigateToHome: vi.fn(),
        } as any;
        navigationServiceSpy.navigateToAuth.mockReturnValue(Promise.resolve());
        navigationServiceSpy.navigateToHome.mockReturnValue(Promise.resolve());

        localizationServiceSpy = {
            applyLanguagePreference: vi.fn(),
            clearStoredLanguage: vi.fn(),
        } as any;
        localizationServiceSpy.applyLanguagePreference.mockReturnValue(Promise.resolve());

        quickMealServiceSpy = { exitPreview: vi.fn() } as any;

        TestBed.configureTestingModule({
            providers: [
                AuthService,
                provideHttpClient(),
                provideHttpClientTesting(),
                { provide: NavigationService, useValue: navigationServiceSpy },
                { provide: LocalizationService, useValue: localizationServiceSpy },
                { provide: QuickMealService, useValue: quickMealServiceSpy },
            ],
        });

        service = TestBed.inject(AuthService);
        httpMock = TestBed.inject(HttpTestingController);
    });

    afterEach(() => {
        httpMock.verify();
        localStorage.clear();
        sessionStorage.clear();
    });

    describe('token management', () => {
        it('should get token from localStorage', () => {
            localStorage.setItem('authToken', 'local-token');
            expect(service.getToken()).toBe('local-token');
        });

        it('should get token from sessionStorage when not in localStorage', () => {
            sessionStorage.setItem('authToken', 'session-token');
            expect(service.getToken()).toBe('session-token');
        });

        it('should prefer localStorage over sessionStorage', () => {
            localStorage.setItem('authToken', 'local-token');
            sessionStorage.setItem('authToken', 'session-token');
            expect(service.getToken()).toBe('local-token');
        });

        it('should return null when no token exists', () => {
            expect(service.getToken()).toBeNull();
        });

        it('should clear all token storage on logout', async () => {
            localStorage.setItem('authToken', 'token');
            sessionStorage.setItem('authToken', 'token');

            await service.onLogout(false);

            expect(localStorage.getItem('authToken')).toBeNull();
            expect(sessionStorage.getItem('authToken')).toBeNull();
        });
    });

    describe('login', () => {
        const loginRequest = new LoginRequest({
            email: 'test@example.com',
            password: 'password123',
            rememberMe: true,
        });

        const fakeToken = createFakeJwt({ nameid: 'user-123', role: 'User' });

        const authResponse = {
            accessToken: fakeToken,
            refreshToken: 'refresh-token-abc',
            user: {
                id: 'user-123',
                email: 'test@example.com',
                isActive: true,
                isEmailConfirmed: true,
                language: 'en',
            },
        };

        it('should POST to /api/v1/auth/login', () => {
            service.login(loginRequest).subscribe();

            const req = httpMock.expectOne(`${authBaseUrl}/login`);
            expect(req.request.method).toBe('POST');
            req.flush(authResponse);
        });

        it('should store tokens on successful login with rememberMe=true', () => {
            service.login(loginRequest).subscribe();

            const req = httpMock.expectOne(`${authBaseUrl}/login`);
            req.flush(authResponse);

            expect(localStorage.getItem('authToken')).toBe(fakeToken);
            expect(localStorage.getItem('refreshToken')).toBe('refresh-token-abc');
        });

        it('should store token in sessionStorage when rememberMe=false', () => {
            const noRememberRequest = new LoginRequest({
                email: 'test@example.com',
                password: 'password123',
                rememberMe: false,
            });

            service.login(noRememberRequest).subscribe();

            const req = httpMock.expectOne(`${authBaseUrl}/login`);
            req.flush(authResponse);

            expect(sessionStorage.getItem('authToken')).toBe(fakeToken);
            expect(localStorage.getItem('authToken')).toBeNull();
        });

        it('should set isAuthenticated to true after login', () => {
            service.login(loginRequest).subscribe();

            const req = httpMock.expectOne(`${authBaseUrl}/login`);
            req.flush(authResponse);

            expect(service.isAuthenticated()).toBe(true);
        });

        it('should extract userId from response and store it', () => {
            service.login(loginRequest).subscribe();

            const req = httpMock.expectOne(`${authBaseUrl}/login`);
            req.flush(authResponse);

            expect(service.getUserId()).toBe('user-123');
            expect(localStorage.getItem('userId')).toBe('user-123');
        });

        it('should apply language preference from response', () => {
            service.login(loginRequest).subscribe();

            const req = httpMock.expectOne(`${authBaseUrl}/login`);
            req.flush(authResponse);

            expect(localizationServiceSpy.applyLanguagePreference).toHaveBeenCalledWith('en');
        });

        it('should not apply language preference when not in response', () => {
            const responseNoLang = {
                ...authResponse,
                user: { ...authResponse.user, language: undefined },
            };

            service.login(loginRequest).subscribe();

            const req = httpMock.expectOne(`${authBaseUrl}/login`);
            req.flush(responseNoLang);

            expect(localizationServiceSpy.applyLanguagePreference).not.toHaveBeenCalled();
        });

        it('should call quickConsumptionService.exitPreview on login', () => {
            service.login(loginRequest).subscribe();

            const req = httpMock.expectOne(`${authBaseUrl}/login`);
            req.flush(authResponse);

            expect(quickMealServiceSpy.exitPreview).toHaveBeenCalled();
        });

        it('should set emailConfirmed from response', () => {
            const responseUnconfirmed = {
                ...authResponse,
                user: { ...authResponse.user, isEmailConfirmed: false },
            };

            service.login(loginRequest).subscribe();

            const req = httpMock.expectOne(`${authBaseUrl}/login`);
            req.flush(responseUnconfirmed);

            expect(service.isEmailConfirmed()).toBe(false);
        });
    });

    describe('register', () => {
        const registerRequest = new RegisterRequest({
            email: 'new@example.com',
            password: 'password123',
            language: 'ru',
        });

        const fakeToken = createFakeJwt({ nameid: 'new-user-456' });

        const authResponse = {
            accessToken: fakeToken,
            refreshToken: 'refresh-new',
            user: {
                id: 'new-user-456',
                email: 'new@example.com',
                isActive: true,
                isEmailConfirmed: true,
            },
        };

        it('should POST to /api/v1/auth/register', () => {
            service.register(registerRequest).subscribe();

            const req = httpMock.expectOne(`${authBaseUrl}/register`);
            expect(req.request.method).toBe('POST');
            req.flush(authResponse);
        });

        it('should store tokens on successful register', () => {
            service.register(registerRequest).subscribe();

            const req = httpMock.expectOne(`${authBaseUrl}/register`);
            req.flush(authResponse);

            expect(service.isAuthenticated()).toBe(true);
            expect(service.getUserId()).toBe('new-user-456');
        });

        it('should store token in sessionStorage (rememberMe=false for register)', () => {
            service.register(registerRequest).subscribe();

            const req = httpMock.expectOne(`${authBaseUrl}/register`);
            req.flush(authResponse);

            expect(sessionStorage.getItem('authToken')).toBe(fakeToken);
            expect(localStorage.getItem('authToken')).toBeNull();
        });
    });

    describe('refreshToken', () => {
        it('should POST to /api/v1/auth/refresh with refresh token', () => {
            localStorage.setItem('refreshToken', 'existing-refresh-token');

            service.refreshToken().subscribe();

            const req = httpMock.expectOne(`${authBaseUrl}/refresh`);
            expect(req.request.method).toBe('POST');
            expect(req.request.body).toEqual({ refreshToken: 'existing-refresh-token' });
            req.flush({
                accessToken: createFakeJwt({ sub: 'user-1' }),
                refreshToken: 'rotated-refresh-token',
                user: { id: 'user-1', email: 'test@example.com', isActive: true, isEmailConfirmed: true },
            });
        });

        it('should update access token on success', () => {
            localStorage.setItem('refreshToken', 'existing-refresh-token');
            const newToken = createFakeJwt({ sub: 'user-1' });

            service.refreshToken().subscribe(result => {
                expect(result).toBe(newToken);
            });

            const req = httpMock.expectOne(`${authBaseUrl}/refresh`);
            req.flush({
                accessToken: newToken,
                refreshToken: 'rotated-refresh-token',
                user: { id: 'user-1', email: 'test@example.com', isActive: true, isEmailConfirmed: true },
            });

            expect(service.isAuthenticated()).toBe(true);
        });

        it('should share a single refresh request across concurrent subscribers', () => {
            localStorage.setItem('refreshToken', 'existing-refresh-token');
            const newToken = createFakeJwt({ sub: 'user-1' });
            const results: Array<string | null> = [];

            service.refreshToken().subscribe(result => results.push(result));
            service.refreshToken().subscribe(result => results.push(result));

            const requests = httpMock.match(`${authBaseUrl}/refresh`);
            expect(requests).toHaveLength(1);

            requests[0].flush({
                accessToken: newToken,
                refreshToken: 'rotated-refresh-token',
                user: { id: 'user-1', email: 'test@example.com', isActive: true, isEmailConfirmed: true },
            });

            expect(results).toEqual([newToken, newToken]);
            expect(localStorage.getItem('refreshToken')).toBe('rotated-refresh-token');
        });

        it('should rotate stored refresh token on success', () => {
            localStorage.setItem('refreshToken', 'existing-refresh-token');

            service.refreshToken().subscribe();

            const req = httpMock.expectOne(`${authBaseUrl}/refresh`);
            req.flush({
                accessToken: createFakeJwt({ sub: 'user-1' }),
                refreshToken: 'rotated-refresh-token',
                user: { id: 'user-1', email: 'test@example.com', isActive: true, isEmailConfirmed: true },
            });

            expect(localStorage.getItem('refreshToken')).toBe('rotated-refresh-token');
        });

        it('should return null and logout when no refresh token exists', () => {
            service.refreshToken().subscribe(result => {
                expect(result).toBeNull();
            });

            httpMock.expectNone(`${authBaseUrl}/refresh`);
            expect(navigationServiceSpy.navigateToAuth).toHaveBeenCalledWith('login');
        });

        it('should logout on refresh failure', () => {
            localStorage.setItem('refreshToken', 'expired-token');

            service.refreshToken().subscribe(result => {
                expect(result).toBeNull();
            });

            const req = httpMock.expectOne(`${authBaseUrl}/refresh`);
            req.flush('Unauthorized', { status: 401, statusText: 'Unauthorized' });

            expect(navigationServiceSpy.navigateToAuth).toHaveBeenCalledWith('login');
        });
    });

    describe('logout', () => {
        beforeEach(() => {
            localStorage.setItem('authToken', 'token');
            localStorage.setItem('refreshToken', 'refresh');
            localStorage.setItem('userId', 'user-1');
            localStorage.setItem('emailConfirmed', 'true');
        });

        it('should clear all auth data', async () => {
            await service.onLogout(false);

            expect(localStorage.getItem('authToken')).toBeNull();
            expect(localStorage.getItem('refreshToken')).toBeNull();
            expect(localStorage.getItem('userId')).toBeNull();
            expect(localStorage.getItem('emailConfirmed')).toBeNull();
        });

        it('should navigate to login when redirectToAuth is true', async () => {
            await service.onLogout(true);

            expect(navigationServiceSpy.navigateToAuth).toHaveBeenCalledWith('login');
            expect(navigationServiceSpy.navigateToHome).not.toHaveBeenCalled();
        });

        it('should navigate to home when redirectToAuth is false', async () => {
            await service.onLogout(false);

            expect(navigationServiceSpy.navigateToHome).toHaveBeenCalled();
            expect(navigationServiceSpy.navigateToAuth).not.toHaveBeenCalled();
        });

        it('should set isAuthenticated to false', async () => {
            // First, ensure authenticated state via signal
            service.initializeAuth();
            expect(service.isAuthenticated()).toBe(true);

            await service.onLogout(false);

            expect(service.isAuthenticated()).toBe(false);
        });

        it('should clear stored language', async () => {
            await service.onLogout(false);

            expect(localizationServiceSpy.clearStoredLanguage).toHaveBeenCalled();
        });
    });

    describe('JWT decoding', () => {
        it('should extract userId from nameid claim', () => {
            const token = createFakeJwt({ nameid: 'user-from-nameid' });
            // Use initializeAuth to test extractUserIdFromToken indirectly
            localStorage.setItem('authToken', token);

            // Create a new service instance to pick up the token
            const freshService = TestBed.inject(AuthService);
            freshService.initializeAuth();
            expect(freshService.getUserId()).toBe('user-from-nameid');
        });

        it('should extract userId from sub claim', () => {
            const token = createFakeJwt({ sub: 'user-from-sub' });
            localStorage.setItem('authToken', token);

            const freshService = TestBed.inject(AuthService);
            freshService.initializeAuth();
            expect(freshService.getUserId()).toBe('user-from-sub');
        });

        it('should extract userId from long nameidentifier claim', () => {
            const token = createFakeJwt({
                'http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier': 'user-long-claim',
            });
            localStorage.setItem('authToken', token);

            const freshService = TestBed.inject(AuthService);
            freshService.initializeAuth();
            expect(freshService.getUserId()).toBe('user-long-claim');
        });

        it('should return null for invalid token', () => {
            localStorage.setItem('authToken', 'not-a-jwt');

            const freshService = TestBed.inject(AuthService);
            freshService.initializeAuth();
            expect(freshService.getUserId()).toBeNull();
        });

        it('should return null for non-JWT string with wrong segment count', () => {
            localStorage.setItem('authToken', 'only.two');

            const freshService = TestBed.inject(AuthService);
            freshService.initializeAuth();
            expect(freshService.getUserId()).toBeNull();
        });

        it('should return null for token with invalid base64 payload', () => {
            localStorage.setItem('authToken', 'header.!!!invalid!!!.signature');

            const freshService = TestBed.inject(AuthService);
            freshService.initializeAuth();
            expect(freshService.getUserId()).toBeNull();
        });
    });

    describe('role checking', () => {
        it('should detect Admin role', () => {
            const token = createFakeJwt({ nameid: 'admin-1', role: 'Admin' });
            localStorage.setItem('authToken', token);

            const freshService = TestBed.inject(AuthService);
            freshService.initializeAuth();
            expect(freshService.isAdmin()).toBe(true);
        });

        it('should detect Premium role', () => {
            const token = createFakeJwt({ nameid: 'premium-1', role: 'Premium' });
            localStorage.setItem('authToken', token);

            const freshService = TestBed.inject(AuthService);
            freshService.initializeAuth();
            expect(freshService.isPremium()).toBe(true);
        });

        it('should return false when no token', () => {
            expect(service.isAdmin()).toBe(false);
            expect(service.isPremium()).toBe(false);
        });

        it('should handle array of roles', () => {
            const token = createFakeJwt({ nameid: 'multi-role', role: ['Admin', 'Premium'] });
            localStorage.setItem('authToken', token);

            const freshService = TestBed.inject(AuthService);
            freshService.initializeAuth();
            expect(freshService.isAdmin()).toBe(true);
            expect(freshService.isPremium()).toBe(true);
        });

        it('should handle single role string', () => {
            const token = createFakeJwt({ nameid: 'user-1', role: 'User' });
            localStorage.setItem('authToken', token);

            const freshService = TestBed.inject(AuthService);
            freshService.initializeAuth();
            expect(freshService.isAdmin()).toBe(false);
            expect(freshService.isPremium()).toBe(false);
        });

        it('should handle roles from Microsoft claims schema', () => {
            const token = createFakeJwt({
                nameid: 'ms-user',
                'http://schemas.microsoft.com/ws/2008/06/identity/claims/role': 'Admin',
            });
            localStorage.setItem('authToken', token);

            const freshService = TestBed.inject(AuthService);
            freshService.initializeAuth();
            expect(freshService.isAdmin()).toBe(true);
        });

        it('should return false when token has no role claims', () => {
            const token = createFakeJwt({ nameid: 'no-roles' });
            localStorage.setItem('authToken', token);

            const freshService = TestBed.inject(AuthService);
            freshService.initializeAuth();
            expect(freshService.isAdmin()).toBe(false);
            expect(freshService.isPremium()).toBe(false);
        });
    });

    describe('computed signals', () => {
        it('should be not authenticated initially when no token exists', () => {
            expect(service.isAuthenticated()).toBe(false);
        });

        it('should compute isEmailConfirmed as true when null (default)', () => {
            // When emailConfirmed is not stored, loadEmailConfirmed returns null,
            // and the computed signal defaults to true via ?? true
            expect(service.isEmailConfirmed()).toBe(true);
        });

        it('should compute isEmailConfirmed as false when set to false', () => {
            service.setEmailConfirmed(false);
            expect(service.isEmailConfirmed()).toBe(false);
        });

        it('should compute isAuthenticated as true when token exists in localStorage', () => {
            const token = createFakeJwt({ sub: 'user-1' });
            localStorage.setItem('authToken', token);

            service.initializeAuth();
            expect(service.isAuthenticated()).toBe(true);
        });

        it('should compute isAuthenticated as true when token exists in sessionStorage', () => {
            const token = createFakeJwt({ sub: 'user-1' });
            sessionStorage.setItem('authToken', token);

            service.initializeAuth();
            expect(service.isAuthenticated()).toBe(true);
        });
    });

    describe('initializeAuth', () => {
        it('should set auth token signal from storage', () => {
            const token = createFakeJwt({ nameid: 'init-user' });
            localStorage.setItem('authToken', token);

            service.initializeAuth();

            expect(service.isAuthenticated()).toBe(true);
            expect(service.getUserId()).toBe('init-user');
        });

        it('should clear userId when no token exists', () => {
            localStorage.setItem('userId', 'stale-user');

            service.initializeAuth();

            expect(localStorage.getItem('userId')).toBeNull();
        });

        it('should resolve userId from token when not stored', () => {
            const token = createFakeJwt({ sub: 'resolved-user' });
            localStorage.setItem('authToken', token);
            // userId not stored

            service.initializeAuth();

            expect(service.getUserId()).toBe('resolved-user');
            expect(localStorage.getItem('userId')).toBe('resolved-user');
        });
    });

    describe('verifyEmail', () => {
        it('should POST to /api/v1/auth/verify-email', () => {
            service.verifyEmail('user-1', 'verify-token').subscribe();

            const req = httpMock.expectOne(`${authBaseUrl}/verify-email`);
            expect(req.request.method).toBe('POST');
            expect(req.request.body).toEqual({ userId: 'user-1', token: 'verify-token' });
            req.flush(true);
        });

        it('should update emailConfirmed on success', () => {
            service.verifyEmail('user-1', 'token').subscribe();

            const req = httpMock.expectOne(`${authBaseUrl}/verify-email`);
            req.flush(true);

            expect(service.isEmailConfirmed()).toBe(true);
        });
    });

    describe('email confirmed storage', () => {
        it('should persist emailConfirmed as true', () => {
            service.setEmailConfirmed(true);

            expect(localStorage.getItem('emailConfirmed')).toBe('true');
            expect(service.isEmailConfirmed()).toBe(true);
        });

        it('should persist emailConfirmed as false', () => {
            service.setEmailConfirmed(false);

            expect(localStorage.getItem('emailConfirmed')).toBe('false');
            expect(service.isEmailConfirmed()).toBe(false);
        });
    });
});
