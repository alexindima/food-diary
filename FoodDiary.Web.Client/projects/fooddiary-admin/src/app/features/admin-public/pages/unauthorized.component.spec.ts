import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { ActivatedRoute, convertToParamMap, Router } from '@angular/router';
import { beforeEach, describe, expect, it, vi } from 'vitest';

import { AdminAuthService } from '../../admin-auth/lib/admin-auth.service';
import { UnauthorizedComponent } from './unauthorized.component';

describe('UnauthorizedComponent', () => {
    let component: UnauthorizedComponent;
    let fixture: ComponentFixture<UnauthorizedComponent>;
    let authService: { tryApplySsoFromReturnUrl: ReturnType<typeof vi.fn> };
    let router: { navigateByUrl: ReturnType<typeof vi.fn> };
    let routeSnapshot: { queryParamMap: ReturnType<typeof convertToParamMap> };

    beforeEach(async () => {
        authService = {
            tryApplySsoFromReturnUrl: vi.fn(),
        };
        router = {
            navigateByUrl: vi.fn(),
        };
        router.navigateByUrl.mockReturnValue(Promise.resolve(true));

        routeSnapshot = {
            queryParamMap: convertToParamMap({
                reason: 'unauthenticated',
                returnUrl: '/users?code=sso-code&page=2',
            }),
        };

        await TestBed.configureTestingModule({
            imports: [UnauthorizedComponent],
            providers: [
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
        expect(component.reason).toBe('unauthenticated');
        expect(component.returnUrl).toBe('/users?code=sso-code&page=2');
    });

    it('should try to recover from sso return url on init', async () => {
        authService.tryApplySsoFromReturnUrl.mockResolvedValue('/users?page=2');

        createComponent();
        fixture.detectChanges();
        await fixture.whenStable();

        expect(authService.tryApplySsoFromReturnUrl).toHaveBeenCalledWith('/users?code=sso-code&page=2');
        expect(router.navigateByUrl).toHaveBeenCalledWith('/users?page=2', { replaceUrl: true });
    });

    it('should not try to recover when reason is forbidden', () => {
        routeSnapshot.queryParamMap = convertToParamMap({
            reason: 'forbidden',
            returnUrl: '/users?page=2',
        });

        createComponent();
        fixture.detectChanges();

        expect(authService.tryApplySsoFromReturnUrl).not.toHaveBeenCalled();
        expect(router.navigateByUrl).not.toHaveBeenCalled();
    });
});
