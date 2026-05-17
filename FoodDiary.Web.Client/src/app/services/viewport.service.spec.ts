import { BreakpointObserver, type BreakpointState } from '@angular/cdk/layout';
import { PLATFORM_ID } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { Subject } from 'rxjs';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';

import { APP_MOBILE_VIEWPORT_QUERY } from '../config/runtime-ui.tokens';
import { ViewportService } from './viewport.service';

const MOBILE_QUERY = '(max-width: 600px)';

let breakpointState$: Subject<BreakpointState>;
let breakpointObserver: { observe: ReturnType<typeof vi.fn> };
let originalMatchMedia: typeof window.matchMedia;

beforeEach(() => {
    breakpointState$ = new Subject<BreakpointState>();
    breakpointObserver = {
        observe: vi.fn().mockReturnValue(breakpointState$),
    };
    originalMatchMedia = window.matchMedia;
    window.matchMedia = vi.fn().mockReturnValue({ matches: false });
});

afterEach(() => {
    window.matchMedia = originalMatchMedia;
});

describe('ViewportService', () => {
    it('uses initial browser match and observes viewport changes', () => {
        window.matchMedia = vi.fn().mockReturnValue({ matches: true });
        const service = setup('browser');

        expect(breakpointObserver.observe).toHaveBeenCalledWith(MOBILE_QUERY);
        expect(service.isMobile()).toBe(true);

        breakpointState$.next({ matches: false, breakpoints: {} });
        expect(service.isMobile()).toBe(false);
    });

    it('uses non-mobile initial value outside browser platform', () => {
        const service = setup('server');

        expect(service.isMobile()).toBe(false);
        expect(window.matchMedia).not.toHaveBeenCalled();
    });
});

function setup(platformId: object | string): ViewportService {
    TestBed.configureTestingModule({
        providers: [
            ViewportService,
            { provide: BreakpointObserver, useValue: breakpointObserver },
            { provide: PLATFORM_ID, useValue: platformId },
            { provide: APP_MOBILE_VIEWPORT_QUERY, useValue: MOBILE_QUERY },
        ],
    });

    const service = TestBed.inject(ViewportService);
    return service;
}
