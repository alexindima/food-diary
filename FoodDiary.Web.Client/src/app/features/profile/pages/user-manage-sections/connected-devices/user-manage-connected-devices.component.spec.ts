import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { TranslateModule } from '@ngx-translate/core';
import { describe, expect, it } from 'vitest';

import type { WebPushSubscriptionItem } from '../../../../../services/notification.service';
import type { ConnectedDeviceViewModel } from '../../user-manage/user-manage.types';
import { UserManageConnectedDevicesComponent } from './user-manage-connected-devices.component';

let fixture: ComponentFixture<UserManageConnectedDevicesComponent>;
let component: UserManageConnectedDevicesComponent;

describe('UserManageConnectedDevicesComponent state', () => {
    it('derives loading, empty, and content states from inputs', async () => {
        await createComponentAsync({ isLoading: true, items: [] });

        expect(component.state()).toBe('loading');

        fixture.componentRef.setInput('isLoading', false);
        fixture.detectChanges();
        expect(component.state()).toBe('empty');

        fixture.componentRef.setInput('items', [createConnectedDeviceItem()]);
        fixture.detectChanges();
        expect(component.state()).toBe('content');
    });
});

type ConnectedDevicesInputs = {
    isLoading: boolean;
    items: ConnectedDeviceViewModel[];
};

async function createComponentAsync(overrides: Partial<ConnectedDevicesInputs> = {}): Promise<void> {
    await TestBed.configureTestingModule({
        imports: [UserManageConnectedDevicesComponent, TranslateModule.forRoot()],
    }).compileComponents();

    fixture = TestBed.createComponent(UserManageConnectedDevicesComponent);
    component = fixture.componentInstance;
    fixture.componentRef.setInput('isLoading', overrides.isLoading ?? false);
    fixture.componentRef.setInput('items', overrides.items ?? []);
    fixture.componentRef.setInput('pushNotificationsBusy', false);
    fixture.componentRef.setInput('removingEndpoint', null);
    fixture.detectChanges();
}

function createConnectedDeviceItem(): ConnectedDeviceViewModel {
    const subscription: WebPushSubscriptionItem = {
        endpoint: 'https://push.example/subscription',
        endpointHost: 'push.example',
        expirationTimeUtc: null,
        locale: 'en',
        userAgent: 'Test Browser',
        createdAtUtc: '2026-05-15T12:00:00Z',
        updatedAtUtc: null,
    };

    return {
        subscription,
        label: 'push.example',
        meta: 'Test Browser',
        isCurrent: true,
    };
}
