import { ChangeDetectionStrategy, Component } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { provideRouter, Router } from '@angular/router';
import { RouterTestingHarness } from '@angular/router/testing';
import { describe, expect, it, vi } from 'vitest';

import { AuthService } from '../../services/auth.service';
import { healthConsentGuard } from './health-consent.guard';
import { PrivacyConsentFacade } from './privacy-consent.facade';

@Component({ template: 'Health fixture', changeDetection: ChangeDetectionStrategy.OnPush })
class HealthRouteFixture {}

@Component({ template: 'Privacy settings fixture', changeDetection: ChangeDetectionStrategy.OnPush })
class PrivacyRouteFixture {}

describe('health consent route protection', () => {
    it.each([
        { authenticated: false, consent: false, target: '/health', text: 'Health fixture', reviews: 0 },
        { authenticated: true, consent: true, target: '/health', text: 'Health fixture', reviews: 1 },
        { authenticated: true, consent: false, target: '/privacy-settings', text: 'Privacy settings fixture', reviews: 1 },
    ])(
        'resolves $target when authenticated=$authenticated and consent=$consent',
        async ({ authenticated, consent, target, text, reviews }) => {
            const ensureHealthConsentAsync = vi.fn<PrivacyConsentFacade['ensureHealthConsentAsync']>().mockResolvedValue(consent);
            TestBed.configureTestingModule({
                providers: [
                    provideRouter([
                        { path: 'health', component: HealthRouteFixture, canActivate: [healthConsentGuard] },
                        { path: 'privacy-settings', component: PrivacyRouteFixture },
                    ]),
                    { provide: AuthService, useValue: { isAuthenticated: (): boolean => authenticated } },
                    { provide: PrivacyConsentFacade, useValue: { ensureHealthConsentAsync } },
                ],
            });
            const harness = await RouterTestingHarness.create();

            await harness.navigateByUrl('/health');

            expect(harness.routeNativeElement?.textContent).toBe(text);
            expect(ensureHealthConsentAsync).toHaveBeenCalledTimes(reviews);
            expect(TestBed.inject(Router).url).toBe(target);
        },
    );
});
