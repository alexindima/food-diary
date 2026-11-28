import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { type Observable, of, Subject, throwError } from 'rxjs';
import { describe, expect, it, type Mock, vi } from 'vitest';

import { provideTranslateTesting } from '../../../../../testing/translate-testing.module';
import { BrowserWindowService } from '../../../../shared/platform/browser-window.service';
import { WearableService } from '../../api/wearable.service';
import type { WearableAuthUrl, WearableConnection } from '../../models/wearable.data';
import { WearableConnectionsComponent } from './wearable-connections';

const CONNECTION: WearableConnection = {
    provider: 'Fitbit',
    externalUserId: 'fitbit-user',
    isActive: true,
    lastSyncedAtUtc: '2026-05-15T00:00:00Z',
    connectedAtUtc: '2026-05-01T00:00:00Z',
};
const PROVIDER_COUNT = 3;

describe('WearableConnectionsComponent loading', () => {
    it('loads provider rows and renders connection status', () => {
        const { component, fixture } = setupComponent();
        const text = getText(fixture);

        expect(component['providerRows']()).toHaveLength(PROVIDER_COUNT);
        expect(component['providerRows']()[0].connection).toEqual(CONNECTION);
        expect(text).toContain('Fitbit');
        expect(text).toContain('WEARABLES.CONNECTED');
        expect(text).toContain('Garmin');
        expect(text).toContain('WEARABLES.NOT_CONNECTED');
    });

    it('shows loading and retry before any connection status is known', async () => {
        const initial = new Subject<WearableConnection[]>();
        const { fixture, service } = setupComponent(undefined, initial);
        const host = fixture.nativeElement as HTMLElement;
        service.getConnections.mockReturnValueOnce(of([]));

        expect(host.querySelector('[role="status"]')).not.toBeNull();
        expect(host.querySelector('.provider-row')).toBeNull();
        initial.error(new Error('Offline'));
        await fixture.whenStable();
        fixture.detectChanges();

        const alert = host.querySelector<HTMLElement>('[role="alert"]');
        expect(alert?.textContent).toContain('WEARABLES.CONNECTIONS_LOAD_ERROR');
        expect(host.querySelector('.provider-row')).toBeNull();
        expect(host.querySelector('[role="status"]')).toBeNull();
        alert?.querySelector<HTMLButtonElement>('button')?.click();
        fixture.detectChanges();

        expect(service.getConnections).toHaveBeenCalledTimes(2);
        expect(host.querySelector('[role="alert"]')).toBeNull();
        expect(host.querySelectorAll('.provider-row')).toHaveLength(PROVIDER_COUNT);
        expect(host.textContent).toContain('WEARABLES.NOT_CONNECTED');
    });

    it('keeps the last known rows if refreshing connections fails', () => {
        const { fixture, service } = setupComponent();
        const host = fixture.nativeElement as HTMLElement;
        service.getConnections.mockReturnValueOnce(throwError(() => new Error('Offline'))).mockReturnValueOnce(of([]));

        host.querySelector<HTMLButtonElement>('.provider-row button')?.click();
        fixture.detectChanges();

        const alert = host.querySelector<HTMLElement>('[role="alert"]');
        expect(alert?.textContent).toContain('WEARABLES.CONNECTIONS_LOAD_ERROR');
        expect(host.querySelector('.provider-row')?.textContent).toContain('WEARABLES.CONNECTED');
        const attemptsBeforeRetry = service.getConnections.mock.calls.length;
        alert?.querySelector<HTMLButtonElement>('button')?.click();
        fixture.detectChanges();

        expect(service.getConnections).toHaveBeenCalledTimes(attemptsBeforeRetry + 1);
        expect(host.querySelector('.provider-row')?.textContent).toContain('WEARABLES.NOT_CONNECTED');
    });
});

describe('WearableConnectionsComponent actions', () => {
    it('delegates disconnect to service and reloads connections', () => {
        const { component, fixture, service } = setupComponent();

        component['disconnect']('Fitbit');
        fixture.detectChanges();

        expect(service.disconnect).toHaveBeenCalledWith('Fitbit');
        expect(service.getConnections).toHaveBeenCalledTimes(2);
    });

    it('supersedes an in-flight refresh when another disconnect succeeds', async () => {
        const garmin = { ...CONNECTION, provider: 'Garmin', externalUserId: 'garmin-user' };
        const { component, fixture, service } = setupComponent(undefined, of([CONNECTION, garmin]));
        const firstRefresh = new Subject<WearableConnection[]>();
        const latestRefresh = new Subject<WearableConnection[]>();
        service.getConnections.mockReturnValueOnce(firstRefresh).mockReturnValueOnce(latestRefresh);

        component['disconnect']('Fitbit');
        fixture.detectChanges();
        expect(firstRefresh.observed).toBe(true);
        expect(component['providerRows']().filter(row => row.connection !== undefined)).toHaveLength(2);

        component['disconnect']('Garmin');
        fixture.detectChanges();
        expect(firstRefresh.observed).toBe(false);
        expect(latestRefresh.observed).toBe(true);

        latestRefresh.next([]);
        latestRefresh.complete();
        firstRefresh.next([garmin]);
        firstRefresh.complete();
        await fixture.whenStable();
        fixture.detectChanges();
        expect(component['providerRows']().every(row => row.connection === undefined)).toBe(true);
    });

    it.each([
        { when: 'while connected', destroyed: false },
        { when: 'after leaving the connection screen', destroyed: true },
    ])('handles the pending Fitbit authorization $when', ({ destroyed }) => {
        const navigate = vi.fn<BrowserWindowService['navigateCurrentTab']>();
        const { fixture, service } = setupComponent(navigate, of([]));
        const authorization = new Subject<WearableAuthUrl>();
        service.getAuthUrl.mockReturnValueOnce(authorization);
        const element = fixture.nativeElement as HTMLElement;
        const row = [...element.querySelectorAll('.provider-row')].find(
            candidate => candidate.querySelector('.provider-name')?.textContent.trim() === 'Fitbit',
        );
        const button = row?.querySelector<HTMLButtonElement>('button');
        expect(button?.textContent).toContain('WEARABLES.CONNECT');
        button?.click();

        expect(service.getAuthUrl).toHaveBeenCalledOnce();
        const [provider, state] = service.getAuthUrl.mock.lastCall ?? [];
        expect(provider).toBe('Fitbit');
        expect(state).toMatch(/^[0-9a-f]{8}-[0-9a-f]{4}-4[0-9a-f]{3}-[89ab][0-9a-f]{3}-[0-9a-f]{12}$/u);
        expect(navigate).not.toHaveBeenCalled();

        if (destroyed) {
            fixture.destroy();
        }
        const authorizationUrl = 'https://provider.example.test/authorize';
        authorization.next({ authorizationUrl });
        authorization.complete();
        if (destroyed) {
            expect(navigate).not.toHaveBeenCalled();
        } else {
            expect(navigate).toHaveBeenCalledExactlyOnceWith(authorizationUrl);
        }
    });
});

function setupComponent(
    navigateCurrentTab = vi.fn<BrowserWindowService['navigateCurrentTab']>(),
    connections: Observable<WearableConnection[]> = of([CONNECTION]),
): {
    component: WearableConnectionsComponent;
    fixture: ComponentFixture<WearableConnectionsComponent>;
    service: {
        disconnect: ReturnType<typeof vi.fn>;
        getAuthUrl: Mock<WearableService['getAuthUrl']>;
        getConnections: ReturnType<typeof vi.fn>;
    };
} {
    const service = {
        disconnect: vi.fn().mockReturnValue(of(void 0)),
        getAuthUrl: vi.fn<WearableService['getAuthUrl']>(),
        getConnections: vi.fn().mockReturnValue(connections),
    };

    TestBed.configureTestingModule({
        imports: [WearableConnectionsComponent],
        providers: [
            provideTranslateTesting(),
            { provide: WearableService, useValue: service },
            { provide: BrowserWindowService, useValue: { navigateCurrentTab } },
        ],
    });

    const fixture = TestBed.createComponent(WearableConnectionsComponent);
    fixture.detectChanges();

    return {
        component: fixture.componentInstance,
        fixture,
        service,
    };
}

function getText(fixture: ComponentFixture<WearableConnectionsComponent>): string {
    return (fixture.nativeElement as HTMLElement).textContent;
}
