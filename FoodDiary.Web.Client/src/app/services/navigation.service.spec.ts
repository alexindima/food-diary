import { TestBed } from '@angular/core/testing';
import { Router } from '@angular/router';
import { NavigationService } from './navigation.service';
import { AuthMode } from '../features/auth/models/auth.data';

describe('NavigationService', () => {
    let service: NavigationService;
    let routerSpy: jasmine.SpyObj<Router>;

    beforeEach(() => {
        routerSpy = jasmine.createSpyObj('Router', ['navigate', 'navigateByUrl']);
        routerSpy.navigate.and.returnValue(Promise.resolve(true));
        routerSpy.navigateByUrl.and.returnValue(Promise.resolve(true));

        TestBed.configureTestingModule({
            providers: [NavigationService, { provide: Router, useValue: routerSpy }],
        });

        service = TestBed.inject(NavigationService);
    });

    it('should navigate to home', async () => {
        await service.navigateToHome();
        expect(routerSpy.navigate).toHaveBeenCalledWith(['/']);
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
