import { signal, type WritableSignal } from '@angular/core';
import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { ActivatedRoute, convertToParamMap, Router } from '@angular/router';
import { beforeEach, describe, expect, it, vi } from 'vitest';

import { provideTranslateTesting } from '../../../../../../../src/testing/translate-testing.module';
import { AdminAuthService } from '../../admin-auth/contracts/admin-session';
import { UnauthorizedComponent } from './unauthorized';

describe('UnauthorizedComponent', () => {
    let component: UnauthorizedComponent;
    let fixture: ComponentFixture<UnauthorizedComponent>;
    let authService: { tryApplySsoFromReturnUrlAsync: ReturnType<typeof vi.fn>; ssoRateLimited: WritableSignal<boolean> };
    let router: { navigateByUrl: ReturnType<typeof vi.fn> };
    let routeSnapshot: { queryParamMap: ReturnType<typeof convertToParamMap> };

    beforeEach(async () => {
        authService = {
            tryApplySsoFromReturnUrlAsync: vi.fn(),
            ssoRateLimited: signal(false),
        };
        router = {
            navigateByUrl: vi.fn(),
        };
        router.navigateByUrl.mockResolvedValue(true);

        routeSnapshot = {
            queryParamMap: convertToParamMap({
                reason: 'unauthenticated',
                returnUrl: '/users?code=sso-code&page=2',
            }),
        };

        await TestBed.configureTestingModule({
            imports: [UnauthorizedComponent],
            providers: [
                provideTranslateTesting(),
                { provide: AdminAuthService, useValue: authService },
                { provide: Router, useValue: router },
                { provide: ActivatedRoute, useValue: { snapshot: routeSnapshot } },
            ],
        }).compileComponents();
    });

    function createComponent(): void {
        fixture = TestBed.createComponent(UnauthorizedComponent);
        component = fixture.componentInstance;
    }

    it('should create', () => {
        createComponent();
        fixture.detectChanges();

        expect(component).toBeTruthy();
        expect(component['reason']).toBe('unauthenticated');
        expect(component['returnUrl']).toBe('/users?code=sso-code&page=2');
    });

    it('should try to recover from sso return url on init', async () => {
        authService.tryApplySsoFromReturnUrlAsync.mockResolvedValue('/users?page=2');

        createComponent();
        fixture.detectChanges();
        await fixture.whenStable();

        expect(authService.tryApplySsoFromReturnUrlAsync).toHaveBeenCalledWith('/users?code=sso-code&page=2');
        expect(router.navigateByUrl).toHaveBeenCalledWith('/users?page=2', { replaceUrl: true });
    });

    it('should not try to recover when reason is forbidden', () => {
        routeSnapshot.queryParamMap = convertToParamMap({
            reason: 'forbidden',
            returnUrl: '/users?page=2',
        });

        createComponent();
        fixture.detectChanges();

        expect(authService.tryApplySsoFromReturnUrlAsync).not.toHaveBeenCalled();
        expect(router.navigateByUrl).not.toHaveBeenCalled();
    });
});

function createRateFeedback(reason: string): HTMLElement {
    TestBed.resetTestingModule();
    TestBed.configureTestingModule({
        imports: [UnauthorizedComponent],
        providers: [
            provideTranslateTesting(),
            { provide: AdminAuthService, useValue: { ssoRateLimited: signal(true), tryApplySsoFromReturnUrlAsync: vi.fn() } },
            { provide: Router, useValue: { navigateByUrl: vi.fn() } },
            { provide: ActivatedRoute, useValue: { snapshot: { queryParamMap: convertToParamMap({ reason, returnUrl: '/users' }) } } },
        ],
    });
    const fixture = TestBed.createComponent(UnauthorizedComponent);
    fixture.detectChanges();
    return fixture.nativeElement as HTMLElement;
}

describe('UnauthorizedComponent throttling feedback', () => {
    it('announces throttling as a temporary sign-in problem', () => {
        const element = createRateFeedback('unauthenticated');
        expect(element.querySelector('h1')?.textContent).toContain('ADMIN_AUTH.RATE_LIMITED_TITLE');
        expect(element.querySelector('[role="alert"]')?.textContent).toContain('ADMIN_AUTH.RATE_LIMITED_MESSAGE');
        expect(element.textContent).not.toContain('permission');
    });

    it('keeps a forbidden response as permission denial despite a previous throttling flag', () => {
        const element = createRateFeedback('forbidden');
        expect(element.textContent).toContain('You do not have permission');
        expect(element.querySelector('[role="alert"]')).toBeNull();
    });
});
