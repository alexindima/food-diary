import { TestBed } from '@angular/core/testing';
import { Router } from '@angular/router';
import { describe, expect, it, vi } from 'vitest';

import { PublicAuthNavigationService } from './public-auth-navigation.service';

describe('PublicAuthNavigationService', () => {
    it('navigates to landing with requested auth mode in query params', async () => {
        const router = {
            url: '/',
            navigate: vi.fn().mockResolvedValue(true),
        };

        TestBed.configureTestingModule({
            providers: [{ provide: Router, useValue: router }],
        });

        const service = TestBed.inject(PublicAuthNavigationService);

        await expect(service.navigateAsync('register')).resolves.toBe(true);

        expect(router.navigate).toHaveBeenCalledWith(['/'], { queryParams: { auth: 'register' } });
    });

    it('preserves the Russian public URL when opening registration', async () => {
        const router = { url: '/ru/calorie-counter', navigate: vi.fn().mockResolvedValue(true) };
        TestBed.configureTestingModule({ providers: [{ provide: Router, useValue: router }] });
        const service = TestBed.inject(PublicAuthNavigationService);
        await service.navigateAsync('register');
        expect(router.navigate).toHaveBeenCalledWith(['/ru'], { queryParams: { auth: 'register' } });
    });
});
