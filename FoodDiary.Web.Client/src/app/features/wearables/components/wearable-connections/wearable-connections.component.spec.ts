import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { TranslateModule } from '@ngx-translate/core';
import { of } from 'rxjs';
import { describe, expect, it, vi } from 'vitest';

import { WearableService } from '../../api/wearable.service';
import type { WearableConnection } from '../../models/wearable.data';
import { WearableConnectionsComponent } from './wearable-connections.component';

const CONNECTION: WearableConnection = {
    provider: 'Fitbit',
    externalUserId: 'fitbit-user',
    isActive: true,
    lastSyncedAtUtc: '2026-05-15T00:00:00Z',
    connectedAtUtc: '2026-05-01T00:00:00Z',
};
const PROVIDER_COUNT = 4;

describe('WearableConnectionsComponent', () => {
    it('loads provider rows and renders connection status', () => {
        const { component, fixture } = setupComponent();
        const text = getText(fixture);

        expect(component.providerRows()).toHaveLength(PROVIDER_COUNT);
        expect(component.providerRows()[0].connection).toEqual(CONNECTION);
        expect(text).toContain('Fitbit');
        expect(text).toContain('WEARABLES.CONNECTED');
        expect(text).toContain('Google Fit');
        expect(text).toContain('WEARABLES.NOT_CONNECTED');
    });

    it('delegates disconnect to service and reloads connections', () => {
        const { component, service } = setupComponent();

        component.disconnect('Fitbit');

        expect(service.disconnect).toHaveBeenCalledWith('Fitbit');
        expect(service.getConnections).toHaveBeenCalledTimes(2);
    });
});

function setupComponent(): {
    component: WearableConnectionsComponent;
    fixture: ComponentFixture<WearableConnectionsComponent>;
    service: {
        disconnect: ReturnType<typeof vi.fn>;
        getAuthUrl: ReturnType<typeof vi.fn>;
        getConnections: ReturnType<typeof vi.fn>;
    };
} {
    const service = {
        disconnect: vi.fn().mockReturnValue(of(void 0)),
        getAuthUrl: vi.fn().mockReturnValue(of({ authorizationUrl: 'https://example.com/oauth' })),
        getConnections: vi.fn().mockReturnValue(of([CONNECTION])),
    };

    TestBed.configureTestingModule({
        imports: [WearableConnectionsComponent, TranslateModule.forRoot()],
        providers: [{ provide: WearableService, useValue: service }],
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
