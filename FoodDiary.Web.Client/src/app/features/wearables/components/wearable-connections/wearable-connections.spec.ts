import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { of, Subject } from 'rxjs';
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

describe('WearableConnectionsComponent', () => {
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

    it('delegates disconnect to service and reloads connections', () => {
        const { component, service } = setupComponent();

        component['disconnect']('Fitbit');

        expect(service.disconnect).toHaveBeenCalledWith('Fitbit');
        expect(service.getConnections).toHaveBeenCalledTimes(2);
    });

    it.each([
        { when: 'while connected', destroyed: false },
        { when: 'after leaving the connection screen', destroyed: true },
    ])('handles the pending Fitbit authorization $when', ({ destroyed }) => {
        const navigate = vi.fn<BrowserWindowService['navigateCurrentTab']>();
        const { fixture, service } = setupComponent(navigate, []);
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
    connections = [CONNECTION],
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
        getConnections: vi.fn().mockReturnValue(of(connections)),
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
