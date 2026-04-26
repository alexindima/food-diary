import { beforeEach, describe, expect, it, vi } from 'vitest';
import { TestBed } from '@angular/core/testing';
import { Router } from '@angular/router';
import { NavigationService } from './navigation.service';
import { AuthMode } from '../features/auth/models/auth.data';

describe('NavigationService', () => {
    let service: NavigationService;
    let routerSpy: any;

    beforeEach(() => {
        routerSpy = { navigate: vi.fn(), navigateByUrl: vi.fn() } as any;
        routerSpy.navigate.mockReturnValue(Promise.resolve(true));
        routerSpy.navigateByUrl.mockReturnValue(Promise.resolve(true));

        TestBed.configureTestingModule({
            providers: [NavigationService, { provide: Router, useValue: routerSpy }],
        });

        service = TestBed.inject(NavigationService);
    });

    it('should navigate to home', async () => {
        await service.navigateToHome();
        expect(routerSpy.navigate).toHaveBeenCalledWith(['/dashboard']);
    });

    it('should navigate to landing', async () => {
        await service.navigateToLanding();
        expect(routerSpy.navigate).toHaveBeenCalledWith(['/']);
    });

    it('should navigate to dashboard when returnUrl is empty', async () => {
        await service.navigateToReturnUrl(null);
        expect(routerSpy.navigateByUrl).toHaveBeenCalledWith('/dashboard');
    });

    it('should navigate to auth with mode and returnUrl', async () => {
        const mode: AuthMode = 'login';
        const returnUrl = '/products';

        await service.navigateToAuth(mode, returnUrl);
        expect(routerSpy.navigate).toHaveBeenCalledWith(['/auth', 'login'], {
            queryParams: { returnUrl: '/products' },
        });
    });

    it('should navigate to auth without returnUrl', async () => {
        const mode: AuthMode = 'register';

        await service.navigateToAuth(mode);
        expect(routerSpy.navigate).toHaveBeenCalledWith(['/auth', 'register'], { queryParams: {} });
    });

    it('should navigate to email verification pending without auto resend', async () => {
        await service.navigateToEmailVerificationPending();
        expect(routerSpy.navigate).toHaveBeenCalledWith(['/verify-pending'], { queryParams: {} });
    });

    it('should navigate to email verification pending with auto resend', async () => {
        await service.navigateToEmailVerificationPending({ autoResend: true });
        expect(routerSpy.navigate).toHaveBeenCalledWith(['/verify-pending'], { queryParams: { autoResend: 'true' } });
    });

    it('should navigate to product edit with id', async () => {
        await service.navigateToProductEdit('abc-123');
        expect(routerSpy.navigate).toHaveBeenCalledWith(['/products/abc-123/edit']);
    });

    it('should navigate to consumption add with mealType', async () => {
        await service.navigateToConsumptionAdd('breakfast');
        expect(routerSpy.navigate).toHaveBeenCalledWith(['/meals/add'], {
            state: { mealType: 'breakfast' },
            queryParams: { mealType: 'breakfast' },
        });
    });

    it('should navigate to profile', async () => {
        await service.navigateToProfile();
        expect(routerSpy.navigate).toHaveBeenCalledWith(['/profile']);
    });
});
