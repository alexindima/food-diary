import { signal } from '@angular/core';
import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { ActivatedRoute, convertToParamMap, Router } from '@angular/router';
import { BehaviorSubject, of } from 'rxjs';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';

import { AuthService } from '../../../../services/auth.service';
import { NavigationService } from '../../../../services/navigation.service';
import { PublicAuthDialogService } from '../../lib/public-auth-dialog.service';
import { MainComponent } from './main';

let fixture: ComponentFixture<MainComponent>;
let authDialogServiceMock: { openAsync: ReturnType<typeof vi.fn> };
let authServiceMock: {
    isAuthenticated: ReturnType<typeof signal<boolean>>;
    isAuthReady: ReturnType<typeof signal<boolean>>;
};
let navigationServiceMock: { navigateToHomeAsync: ReturnType<typeof vi.fn> };
let queryParamMapSubject: BehaviorSubject<ReturnType<typeof convertToParamMap>>;
let routerMock: { navigate: ReturnType<typeof vi.fn> };
let routeStub: object;

beforeEach(() => {
    TestBed.resetTestingModule();
    authDialogServiceMock = {
        openAsync: vi.fn().mockResolvedValue({ afterClosed: () => of(void 0) }),
    };
    authServiceMock = {
        isAuthenticated: signal(false),
        isAuthReady: signal(true),
    };
    navigationServiceMock = {
        navigateToHomeAsync: vi.fn().mockResolvedValue(void 0),
    };
    queryParamMapSubject = new BehaviorSubject(convertToParamMap({}));
    routerMock = { navigate: vi.fn().mockResolvedValue(true) };
});

afterEach(() => {
    vi.clearAllMocks();
});

async function createComponentAsync(
    path: string,
    params: Record<string, string> = {},
    queryParams: Record<string, string> = {},
): Promise<void> {
    TestBed.overrideComponent(MainComponent, {
        set: {
            imports: [],
            template: '',
        },
    });

    routeStub = {
        queryParamMap: queryParamMapSubject.asObservable(),
        snapshot: {
            routeConfig: { path },
            paramMap: convertToParamMap(params),
            queryParamMap: convertToParamMap(queryParams),
        },
    };

    await TestBed.configureTestingModule({
        imports: [MainComponent],
        providers: [
            { provide: PublicAuthDialogService, useValue: authDialogServiceMock },
            { provide: AuthService, useValue: authServiceMock },
            { provide: NavigationService, useValue: navigationServiceMock },
            { provide: Router, useValue: routerMock },
            { provide: ActivatedRoute, useValue: routeStub },
        ],
    }).compileComponents();

    fixture = TestBed.createComponent(MainComponent);
}

describe('MainComponent', () => {
    it('opens auth dialog from auth query param with return query params', async () => {
        await createComponentAsync('', {}, { auth: 'login', returnUrl: '/dashboard', adminReturnUrl: '/users' });
        queryParamMapSubject.next(convertToParamMap({ auth: 'login', returnUrl: '/dashboard', adminReturnUrl: '/users' }));

        fixture.detectChanges();
        await vi.waitFor(() => {
            expect(authDialogServiceMock.openAsync).toHaveBeenCalledTimes(1);
        });
        expect(authDialogServiceMock.openAsync).toHaveBeenCalledWith(
            expect.objectContaining({
                mode: 'login',
                returnUrl: '/dashboard',
                adminReturnUrl: '/users',
            }),
        );
    });

    it('opens register dialog from auth query param', async () => {
        await createComponentAsync('', {}, { auth: 'register' });
        queryParamMapSubject.next(convertToParamMap({ auth: 'register' }));

        fixture.detectChanges();
        await vi.waitFor(() => {
            expect(authDialogServiceMock.openAsync).toHaveBeenCalledTimes(1);
        });
        expect(authDialogServiceMock.openAsync).toHaveBeenCalledWith(
            expect.objectContaining({
                mode: 'register',
                returnUrl: null,
                adminReturnUrl: null,
            }),
        );
    });

    it('does not reopen auth dialog for duplicate auth query param emissions', async () => {
        const authParams = { auth: 'login', returnUrl: '/dashboard' };
        await createComponentAsync('', {}, authParams);
        queryParamMapSubject.next(convertToParamMap(authParams));

        fixture.detectChanges();
        await vi.waitFor(() => {
            expect(authDialogServiceMock.openAsync).toHaveBeenCalledTimes(1);
        });

        queryParamMapSubject.next(convertToParamMap(authParams));

        expect(authDialogServiceMock.openAsync).toHaveBeenCalledTimes(1);
    });

    it('does not open auth dialog without auth query param', async () => {
        await createComponentAsync('');

        fixture.detectChanges();

        expect(authDialogServiceMock.openAsync).not.toHaveBeenCalled();
    });

    it('redirects to dashboard if landing is mounted while authenticated', async () => {
        authServiceMock.isAuthenticated.set(true);
        await createComponentAsync('');

        fixture.detectChanges();

        expect(navigationServiceMock.navigateToHomeAsync).toHaveBeenCalled();
    });

    it('clears auth query params when dialog closes', async () => {
        await createComponentAsync('', {}, { auth: 'login', returnUrl: '/dashboard' });
        queryParamMapSubject.next(convertToParamMap({ auth: 'login', returnUrl: '/dashboard' }));

        fixture.detectChanges();
        await vi.waitFor(() => {
            expect(routerMock.navigate).toHaveBeenCalledTimes(1);
        });
        expect(routerMock.navigate).toHaveBeenCalledWith([], {
            relativeTo: routeStub,
            queryParams: { auth: null, returnUrl: null, adminReturnUrl: null },
            queryParamsHandling: 'merge',
            replaceUrl: true,
        });
    });

    it('does not clear auth query params after successful authentication redirects away', async () => {
        authServiceMock.isAuthenticated.set(true);
        await createComponentAsync('', {}, { auth: 'login', returnUrl: '/dashboard' });
        queryParamMapSubject.next(convertToParamMap({ auth: 'login', returnUrl: '/dashboard' }));

        fixture.detectChanges();
        await vi.waitFor(() => {
            expect(authDialogServiceMock.openAsync).toHaveBeenCalledTimes(1);
        });

        expect(routerMock.navigate).not.toHaveBeenCalled();
        expect(navigationServiceMock.navigateToHomeAsync).toHaveBeenCalled();
    });
});

describe('MainComponent auth dialog cancellation', () => {
    it('allows a later auth dialog when lazy dialog loading is cancelled', async () => {
        authDialogServiceMock.openAsync.mockResolvedValueOnce(null).mockResolvedValueOnce({ afterClosed: () => of(void 0) });
        await createComponentAsync('', {}, { auth: 'login' });
        queryParamMapSubject.next(convertToParamMap({ auth: 'login' }));

        fixture.detectChanges();
        await vi.waitFor(() => {
            expect(authDialogServiceMock.openAsync).toHaveBeenCalledTimes(1);
        });

        queryParamMapSubject.next(convertToParamMap({ auth: 'register' }));

        await vi.waitFor(() => {
            expect(authDialogServiceMock.openAsync).toHaveBeenCalledTimes(2);
        });
        expect(authDialogServiceMock.openAsync).toHaveBeenLastCalledWith(expect.objectContaining({ mode: 'register' }));
    });
});
