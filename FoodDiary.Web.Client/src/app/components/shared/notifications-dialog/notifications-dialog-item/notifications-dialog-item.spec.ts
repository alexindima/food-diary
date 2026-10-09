import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { describe, expect, it, vi } from 'vitest';

import { provideTranslateTesting } from '../../../../../testing/translate-testing.module';
import { decodeNotificationKind } from '../../../../shared/models/notification-kind';
import { utcInstant } from '../../../../shared/models/semantics/date-value';
import { entityId } from '../../../../shared/models/semantics/entity-id';
import type { NotificationItem } from '../../../../shared/notifications/notification.service';
import type { NotificationViewModel } from '../notifications-dialog-lib/notifications-dialog.types';
import { NotificationsDialogItemComponent } from './notifications-dialog-item';

async function setupNotificationsDialogItemAsync(): Promise<ComponentFixture<NotificationsDialogItemComponent>> {
    await TestBed.configureTestingModule({
        imports: [NotificationsDialogItemComponent],
        providers: [provideTranslateTesting()],
    }).compileComponents();

    const fixture = TestBed.createComponent(NotificationsDialogItemComponent);
    fixture.componentRef.setInput('item', createNotificationViewModel());
    return fixture;
}

describe('NotificationsDialogItemComponent', () => {
    it('emits notification when opened', async () => {
        const fixture = await setupNotificationsDialogItemAsync();
        const component = fixture.componentInstance;
        const openSpy = vi.fn<(notification: NotificationItem) => void>();
        component['notificationOpen'].subscribe(notification => {
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
            id: entityId<'notification'>('n1'),
            type: decodeNotificationKind('PasswordSetupSuggested'),
            title: 'Title',
            body: 'Body',
            targetUrl: '/profile',
            referenceId: 'ref',
            isRead: false,
            createdAtUtc: utcInstant('2026-05-17T00:00:00Z'),
        },
        isPasswordSetupSuggestion: true,
        isDietologistInvitation: false,
        isDietologistRecommendation: false,
        hasAccentIcon: true,
        icon: 'password',
        badgeKey: 'BADGE',
        actionKey: 'ACTION',
        ariaLabel: 'Open notification',
        dateLabel: 'Today',
    };
}
