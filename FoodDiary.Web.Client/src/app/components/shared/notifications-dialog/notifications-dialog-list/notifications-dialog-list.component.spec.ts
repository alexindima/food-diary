import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { TranslateModule } from '@ngx-translate/core';
import { describe, expect, it, vi } from 'vitest';

import type { NotificationItem } from '../../../../services/notification.service';
import type { NotificationViewModel } from '../notifications-dialog-lib/notifications-dialog.types';
import { NotificationsDialogListComponent } from './notifications-dialog-list.component';

async function setupNotificationsDialogListAsync(): Promise<ComponentFixture<NotificationsDialogListComponent>> {
    await TestBed.configureTestingModule({
        imports: [NotificationsDialogListComponent, TranslateModule.forRoot()],
    }).compileComponents();

    const fixture = TestBed.createComponent(NotificationsDialogListComponent);
    fixture.componentRef.setInput('items', [createNotificationViewModel()]);
    return fixture;
}

describe('NotificationsDialogListComponent', () => {
    it('forwards opened notification from item', async () => {
        const fixture = await setupNotificationsDialogListAsync();
        const component = fixture.componentInstance;
        const openSpy = vi.fn<(notification: NotificationItem) => void>();
        component.notificationOpen.subscribe(notification => {
            openSpy(notification);
        });
        fixture.detectChanges();

        const button = (fixture.nativeElement as HTMLElement).querySelector<HTMLButtonElement>('.notifications-dialog__item');
        button?.click();

        expect(openSpy).toHaveBeenCalledWith(createNotificationViewModel().notification);
    });
});

function createNotificationViewModel(): NotificationViewModel {
    return {
        notification: {
            id: 'n1',
            type: 'PasswordSetupSuggested',
            title: 'Title',
            body: 'Body',
            targetUrl: '/profile',
            referenceId: 'ref',
            isRead: false,
            createdAtUtc: '2026-05-17T00:00:00Z',
        },
        isPasswordSetupSuggestion: true,
        isDietologistInvitation: false,
        hasAccentIcon: true,
        icon: 'password',
        badgeKey: 'BADGE',
        actionKey: 'ACTION',
        ariaLabel: 'Open notification',
        dateLabel: 'Today',
    };
}
