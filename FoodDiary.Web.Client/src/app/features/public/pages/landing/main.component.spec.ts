import { signal } from '@angular/core';
import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { ActivatedRoute, convertToParamMap, Router } from '@angular/router';
import { BehaviorSubject, of } from 'rxjs';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';

import { AuthService } from '../../../../services/auth.service';
import { NavigationService } from '../../../../services/navigation.service';
import { PublicAuthDialogService } from '../../lib/public-auth-dialog.service';
import { MainComponent } from './main.component';

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
        openAsync: vi.fn().mockResolvedValue({ afterClosed: () => of(undefined) }),
    };
    authServiceMock = {
        isAuthenticated: signal(false),
        isAuthReady: signal(true),
    };
    navigationServiceMock = {
        navigateToHomeAsync: vi.fn().mockResolvedValue(undefined),
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
        expect(authDialogServiceMock.openAsync).toHaveBeenCalledWith({
            mode: 'login',
            returnUrl: '/dashboard',
            adminReturnUrl: '/users',
        });
    });

    it('opens register dialog from auth query param', async () => {
        await createComponentAsync('', {}, { auth: 'register' });
        queryParamMapSubject.next(convertToParamMap({ auth: 'register' }));

        fixture.detectChanges();
        await vi.waitFor(() => {
            expect(authDialogServiceMock.openAsync).toHaveBeenCalledTimes(1);
        });
        expect(authDialogServiceMock.openAsync).toHaveBeenCalledWith({
            mode: 'register',
            returnUrl: null,
            adminReturnUrl: null,
        });
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
});
