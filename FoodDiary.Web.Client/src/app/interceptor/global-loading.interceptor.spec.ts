import { HTTP_INTERCEPTORS, HttpClient, HttpContext, provideHttpClient, withInterceptorsFromDi } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';

import { FORCE_GLOBAL_LOADING, SKIP_GLOBAL_LOADING } from '../constants/global-loading-context.tokens';
import { GlobalLoadingService } from '../services/global-loading.service';
import { GlobalLoadingInterceptor } from './global-loading.interceptor';

describe('GlobalLoadingInterceptor', () => {
    let http: HttpClient;
    let httpTesting: HttpTestingController;
    let globalLoadingService: GlobalLoadingService;

    beforeEach(() => {
        vi.useFakeTimers();

        TestBed.configureTestingModule({
            providers: [
                provideHttpClient(withInterceptorsFromDi()),
                provideHttpClientTesting(),
                { provide: HTTP_INTERCEPTORS, useClass: GlobalLoadingInterceptor, multi: true },
                GlobalLoadingService,
            ],
        });

        http = TestBed.inject(HttpClient);
        httpTesting = TestBed.inject(HttpTestingController);
        globalLoadingService = TestBed.inject(GlobalLoadingService);
        vi.spyOn(globalLoadingService, 'trackRequest');
    });

    afterEach(() => {
        httpTesting.verify();
        vi.useRealTimers();
    });

    it('should track GET requests by default', () => {
        http.get('/api/meals').subscribe();

        const req = httpTesting.expectOne('/api/meals');
        req.flush({});

        expect(globalLoadingService.trackRequest).toHaveBeenCalledTimes(1);
    });

    it('should skip tracking when SKIP_GLOBAL_LOADING context is true', () => {
        http.get('/api/meals', { context: new HttpContext().set(SKIP_GLOBAL_LOADING, true) }).subscribe();

        const req = httpTesting.expectOne('/api/meals');
        req.flush({});

        expect(globalLoadingService.trackRequest).not.toHaveBeenCalled();
    });

    it('should not track POST requests by default', () => {
        http.post('/api/meals', {}).subscribe();

        const req = httpTesting.expectOne('/api/meals');
        req.flush({});

        expect(globalLoadingService.trackRequest).not.toHaveBeenCalled();
    });

    it('should allow forcing tracking for non-GET requests', () => {
        http.post('/api/meals', {}, { context: new HttpContext().set(FORCE_GLOBAL_LOADING, true) }).subscribe();

        const req = httpTesting.expectOne('/api/meals');
        req.flush({});

        expect(globalLoadingService.trackRequest).toHaveBeenCalledTimes(1);
    });
});
