import { ApplicationInitStatus } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { SwRegistrationOptions } from '@angular/service-worker';
import { firstValueFrom, Subject } from 'rxjs';
import { describe, expect, it, vi } from 'vitest';

import { appConfig } from './app.config';
import { LocalWebmcpPilotService } from './composition/webmcp/local-webmcp-pilot.service';
import { AppBootstrapService } from './shared/bootstrap/app-bootstrap.service';
import { PrivacyConsentFacade } from './shared/privacy/privacy-consent.facade';
import { AppUpdateService } from './shared/service-worker/app-update.service';
import { PwaInstallService } from './shared/service-worker/pwa-install.service';
import { PwaInstallPromotionService } from './shared/service-worker/pwa-install-promotion.service';

describe('Client application startup providers', () => {
    it('initializes theme, privacy and PWA services while session restoration gates app rendering', async () => {
        const sessionRestored = new Subject<void>();
        const startup = {
            initializeTheme: vi.fn(),
            initializeSessionAsync: vi.fn(async () => firstValueFrom(sessionRestored)),
            initializeObservability: vi.fn(),
        };
        const establishPrivacy = vi.fn(() => ({}));
        const pwa = { initialize: vi.fn() };
        const promotion = { initialize: vi.fn() };
        const updates = { initialize: vi.fn() };
        TestBed.configureTestingModule({
            providers: [
                ...appConfig.providers,
                { provide: AppBootstrapService, useValue: startup },
                { provide: PrivacyConsentFacade, useFactory: establishPrivacy },
                { provide: PwaInstallService, useValue: pwa },
                { provide: PwaInstallPromotionService, useValue: promotion },
                { provide: AppUpdateService, useValue: updates },
                { provide: LocalWebmcpPilotService, useValue: { initialize: vi.fn() } },
            ],
        });

        const appInitialization = TestBed.inject(ApplicationInitStatus);
        try {
            expect(startup.initializeTheme).toHaveBeenCalledOnce();
            expect(startup.initializeSessionAsync).toHaveBeenCalledOnce();
            expect(startup.initializeTheme).toHaveBeenCalledBefore(startup.initializeSessionAsync);
            expect(establishPrivacy).toHaveBeenCalledOnce();
            expect(startup.initializeObservability).toHaveBeenCalledOnce();
            expect(establishPrivacy).toHaveBeenCalledBefore(startup.initializeObservability);
            expect(pwa.initialize).toHaveBeenCalledOnce();
            expect(promotion.initialize).toHaveBeenCalledOnce();
            expect(updates.initialize).toHaveBeenCalledOnce();
            expect(appInitialization.done).toBe(false);
            expect(TestBed.inject(SwRegistrationOptions)).toMatchObject({
                enabled: false,
                registrationStrategy: 'registerWhenStable:30000',
            });
        } finally {
            sessionRestored.next();
            sessionRestored.complete();
            await appInitialization.donePromise;
        }
        expect(appInitialization.done).toBe(true);
    });
});
